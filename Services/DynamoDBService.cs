using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ARISESLCOM.DTO;
using ARISESLCOM.Services.interfaces;
using System.Text.Json;

namespace ARISESLCOM.Services
{
    public class DynamoDBService : IDynamoDBService
    {
        private readonly IAmazonDynamoDB _dynamoDBClient;
        private readonly IConfiguration _configuration;

        public DynamoDBService(IAmazonDynamoDB dynamoDBClient, IConfiguration configuration)
        {
            _dynamoDBClient = dynamoDBClient;
            _configuration = configuration;
        }

        private string GetTableName()
        {
            return _configuration["AWS:TrackerPedidos:TableName"] ?? "tracker-pedidos";
        }

        public async Task<List<TrackerPedidoViewModel>> GetAllTrackingPedidosAsync()
        {
            var result = new List<TrackerPedidoViewModel>();

            try
            {
                var tableName = GetTableName();
                
                // Scan completo da tabela
                var scanRequest = new ScanRequest
                {
                    TableName = tableName
                };

                ScanResponse? scanResponse = null;
                do
                {
                    if (scanResponse != null)
                    {
                        scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                    }

                    scanResponse = await _dynamoDBClient.ScanAsync(scanRequest);

                    foreach (var item in scanResponse.Items)
                    {
                        var trackerPedido = MapDynamoDBItemToDTO(item);
                        if (trackerPedido != null)
                        {
                            var viewModel = MapToViewModel(trackerPedido);
                            if (viewModel != null)
                            {
                                result.Add(viewModel);
                            }
                        }
                    }
                } while (scanResponse.LastEvaluatedKey.Count > 0);

                // Filtrar apenas últimos 15 dias
                var dataLimite = DateTime.Now.AddDays(-15);
                var resultadoFiltrado = result.Where(r => 
                    r.DataAtualizacao.HasValue && r.DataAtualizacao.Value >= dataLimite
                ).ToList();

                // Separar por tipo de envio e ordenar por updated_at decrescente
                var correios = resultadoFiltrado.Where(r => r.TipoEnvio == "C")
                    .OrderByDescending(r => r.DataAtualizacao ?? DateTime.MinValue)
                    .ToList();

                var buslog = resultadoFiltrado.Where(r => r.TipoEnvio == "B")
                    .OrderByDescending(r => r.DataAtualizacao ?? DateTime.MinValue)
                    .ToList();

                return correios.Concat(buslog).ToList();
            }
            catch (Exception ex)
            {
                // Log error
                throw new Exception($"Erro ao ler tabela DynamoDB: {ex.Message}", ex);
            }
        }

        private TrackerPedidoDTO? MapDynamoDBItemToDTO(Dictionary<string, AttributeValue> item)
        {
            try
            {
                var dto = new TrackerPedidoDTO();

                if (item.ContainsKey("id_pedido"))
                {
                    dto.IdPedido = item["id_pedido"].S;
                }

                if (item.ContainsKey("cod_rastreamento"))
                {
                    dto.CodRastreamento = item["cod_rastreamento"].S;
                }

                if (item.ContainsKey("nome"))
                {
                    dto.Nome = item["nome"].S;
                }

                if (item.ContainsKey("tipo_envio"))
                {
                    dto.TipoEnvio = item["tipo_envio"].S;
                }

                if (item.ContainsKey("rastreamento_json"))
                {
                    var rastreamentoJsonAttr = item["rastreamento_json"];
                    if (rastreamentoJsonAttr.S != null)
                    {
                        dto.RastreamentoJson = rastreamentoJsonAttr.S;
                    }
                    else if (rastreamentoJsonAttr.M != null)
                    {
                        // Se for um mapa, converter para JSON string
                        dto.RastreamentoJson = System.Text.Json.JsonSerializer.Serialize(rastreamentoJsonAttr.M.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.S ?? kvp.Value.N ?? (kvp.Value.BOOL ? "true" : (kvp.Value.IsBOOLSet ? "false" : "null"))
                        ));
                    }
                }

                if (item.ContainsKey("updated_at"))
                {
                    if (item["updated_at"].N != null && long.TryParse(item["updated_at"].N, out long unixTimestamp))
                    {
                        dto.UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
                    }
                    else if (item["updated_at"].S != null && DateTime.TryParse(item["updated_at"].S, out DateTime dateTime))
                    {
                        dto.UpdatedAt = dateTime;
                    }
                }

                return dto;
            }
            catch
            {
                return null;
            }
        }

        private TrackerPedidoViewModel? MapToViewModel(TrackerPedidoDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.TipoEnvio))
                return null;

            var viewModel = new TrackerPedidoViewModel
            {
                NomeCliente = dto.Nome ?? string.Empty,
                DataAtualizacao = dto.UpdatedAt,
                TipoEnvio = dto.TipoEnvio,
                Via = dto.TipoEnvio ?? string.Empty
            };

            if (dto.TipoEnvio == "C") // Correios
            {
                viewModel.NumPedido = dto.IdPedido ?? string.Empty;
                viewModel.Rastreamento = dto.CodRastreamento ?? string.Empty;
                // Para tipo_envio=C, não preencher origem ainda
                viewModel.Origem = string.Empty;
                
                // Pegar primeira ocorrência (mais recente) do JSON
                if (!string.IsNullOrEmpty(dto.RastreamentoJson))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(dto.RastreamentoJson);
                        if (jsonDoc.RootElement.TryGetProperty("objetos", out var objetos) && 
                            objetos.GetArrayLength() > 0)
                        {
                            var primeiroObjeto = objetos[0];
                            if (primeiroObjeto.TryGetProperty("eventos", out var eventos) && 
                                eventos.GetArrayLength() > 0)
                            {
                                var primeiroEvento = eventos[0];
                                if (primeiroEvento.TryGetProperty("descricao", out var descricao))
                                {
                                    viewModel.Status = descricao.GetString() ?? string.Empty;
                                }
                            }
                        }
                    }
                    catch
                    {
                        viewModel.Status = "Status não disponível";
                    }
                }
            }
            else if (dto.TipoEnvio == "B") // Buslog
            {
                // Processar cod_rastreamento como JSON para extrair SiteOrderId ou OrderId
                if (!string.IsNullOrEmpty(dto.CodRastreamento))
                {
                    try
                    {
                        var codRastreamentoDoc = JsonDocument.Parse(dto.CodRastreamento);
                        
                        // Verificar se SiteOrderId está preenchido
                        if (codRastreamentoDoc.RootElement.TryGetProperty("SiteOrderId", out var siteOrderId))
                        {
                            var siteOrderIdValue = siteOrderId.GetString();
                            if (!string.IsNullOrWhiteSpace(siteOrderIdValue))
                            {
                                viewModel.NumPedido = siteOrderIdValue;
                                viewModel.Origem = "E-commerce";
                            }
                            else
                            {
                                // Se SiteOrderId vazio, usar OrderId
                                if (codRastreamentoDoc.RootElement.TryGetProperty("OrderId", out var orderId))
                                {
                                    viewModel.NumPedido = orderId.GetString() ?? string.Empty;
                                }
                                else
                                {
                                    viewModel.NumPedido = dto.CodRastreamento;
                                }
                                viewModel.Origem = "ERP-Lion";
                            }
                        }
                        else
                        {
                            // Se não tiver SiteOrderId, usar OrderId
                            if (codRastreamentoDoc.RootElement.TryGetProperty("OrderId", out var orderId))
                            {
                                viewModel.NumPedido = orderId.GetString() ?? string.Empty;
                            }
                            else
                            {
                                viewModel.NumPedido = dto.CodRastreamento;
                            }
                            viewModel.Origem = "ERP-Lion";
                        }
                    }
                    catch
                    {
                        // Se não for JSON válido, usar cod_rastreamento como está
                        viewModel.NumPedido = dto.CodRastreamento ?? string.Empty;
                        viewModel.Origem = "ERP-Lion";
                    }
                }
                else
                {
                    viewModel.NumPedido = string.Empty;
                    viewModel.Origem = "ERP-Lion";
                }
                
                // Processar rastreamento_json para pegar encomenda e última ocorrência
                if (!string.IsNullOrEmpty(dto.RastreamentoJson))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(dto.RastreamentoJson);
                        
                        // Pegar campo encomenda
                        if (jsonDoc.RootElement.TryGetProperty("encomenda", out var encomenda))
                        {
                            viewModel.Rastreamento = encomenda.GetString() ?? string.Empty;
                        }
                        
                        // Pegar ocorrência com data mais recente do array "ocorrencias"
                        if (jsonDoc.RootElement.TryGetProperty("ocorrencias", out var ocorrenciasArray))
                        {
                            if (ocorrenciasArray.ValueKind == JsonValueKind.Array)
                            {
                                var ocorrenciasList = ocorrenciasArray.EnumerateArray().ToList();
                                if (ocorrenciasList.Count > 0)
                                {
                                    // Ordenar por data decrescente e pegar a mais recente
                                    var ocorrenciaMaisRecente = ocorrenciasList
                                        .OrderByDescending(oc =>
                                        {
                                            if (oc.TryGetProperty("data", out var dataProp))
                                            {
                                                var dataStr = dataProp.GetString();
                                                if (!string.IsNullOrWhiteSpace(dataStr) && 
                                                    DateTime.TryParseExact(dataStr, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var data))
                                                {
                                                    return data;
                                                }
                                            }
                                            return DateTime.MinValue;
                                        })
                                        .FirstOrDefault();
                                    
                                    if (ocorrenciaMaisRecente.ValueKind != JsonValueKind.Undefined)
                                    {
                                        if (ocorrenciaMaisRecente.TryGetProperty("descricao", out var descricao))
                                        {
                                            viewModel.Status = descricao.GetString() ?? string.Empty;
                                        }
                                        else if (ocorrenciaMaisRecente.TryGetProperty("codigo", out var codigo))
                                        {
                                            viewModel.Status = codigo.GetString() ?? string.Empty;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        viewModel.Status = "Status não disponível";
                    }
                }
            }

            return viewModel;
        }
    }
}

