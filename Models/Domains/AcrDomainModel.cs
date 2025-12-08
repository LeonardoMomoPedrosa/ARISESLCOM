using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Services.interfaces;
using ARISESLCOM.DTO.Rede;
using System.Data;
using System.Text.Json;
using ARISESLCOM.Helpers;
using SLCOMLIB.Helpers;

namespace ARISESLCOM.Models.Domains
{
    public class AcrDomainModel(IConfiguration configuration,
                                IRedisCacheService cache,
                                IOrderDomainModel orderDomain,
                                ISiteApiServices siteApi) : AcrDB(cache), IAcrDomainModel
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IOrderDomainModel _orderDomain = orderDomain;
        private ISiteApiServices _siteApi = siteApi;

        public async Task<List<AcrOrderViewModel>> GetCreditCardOrdersForProcessingAsync()
        {
            try
            {
                return await GetAcrCardsDBAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao buscar pedidos para processamento: {ex.Message}", ex);
            }
        }

        public async Task<AcrOrderInfo> GetOrderInfoAsync(int orderId)
        {
            try
            {
                // Buscar TODOS os dados em uma única query
                var orderData = await GetOrderInfoDBAsync(orderId);

                if (orderData.Rows.Count == 0)
                    throw new Exception("Pedido não encontrado");

                var row = orderData.Rows[0];

                return new AcrOrderInfo
                {
                    OrderId = Convert.ToInt32(row["PKId"]),
                    UserId = Convert.ToInt32(row["PKIdUsuario"]),
                    CustomerName = row["nome"]?.ToString() ?? "",
                    CardData = row["aa"]?.ToString() ?? "",
                    CardExpiry = row["val"]?.ToString() ?? "",
                    ParcVal = row["parcVal"] != DBNull.Value ? Convert.ToDecimal(row["parcVal"]) : 0,
                    ShippingAmount = row["frete"] != DBNull.Value ? Convert.ToDecimal(row["frete"]) : 0,
                    Installments = Convert.ToInt32(row["parc"]),

                    // Status do pagamento
                    PaymentStatus = row["REDESTATUS"]?.ToString() ?? "",
                    PaymentStatusDescription = row["REDESTATUSDESC"]?.ToString() ?? "",
                    PaymentTid = row["TID"]?.ToString() ?? "",
                    PaymentAuthCode = row["AUTHCODE"]?.ToString() ?? "",
                    PaymentStep = row["STEP"] != DBNull.Value ? Convert.ToInt32(row["STEP"]) : 1,

                    // Status do frete
                    ShippingStatus = row["REDESTATUS_SHIP"]?.ToString() ?? "",
                    ShippingStatusDescription = row["REDESTATUSDESC_SHIP"]?.ToString() ?? "",
                    ShippingTid = row["TID_SHIP"]?.ToString() ?? "",
                    ShippingAuthCode = row["AUTHCODE_SHIP"]?.ToString() ?? "",
                    ShippingStep = row["STEP_SHIP"] != DBNull.Value ? Convert.ToInt32(row["STEP_SHIP"]) : 1
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao buscar informações do pedido: {ex.Message}", ex);
            }
        }

        public async Task<AcrProcessResult> ProcessPaymentAsync(int orderId, string chargeType, IRedeService redeService)
        {
            var result = new AcrProcessResult();

            try
            {
                var orderInfo = await GetOrderInfoAsync(orderId);
                bool isShipping = chargeType.Equals("FRETE");
                bool isFull = chargeType.Equals("PRODUTO+FRETE");

                var currentStepLabel = isShipping ? "STEP_SHIP" : "STEP";

                // Verificar se já existe TID cadastrado para este tipo de cobrança
                string existingTid = isShipping ? orderInfo.ShippingTid : orderInfo.PaymentTid;
                
                // Construir o reference/orderId usado na transação original
                string reference = isShipping ? $"S{orderId}" : orderId.ToString();

                RedeAuthorizationResult authResult = null;

                // Tentar recuperar transação existente antes de criar uma nova
                RedeGetTransactionResponse getTransactionResult = null;
                
                // 1. Se existe TID, tentar recuperar usando TID
                if (!string.IsNullOrEmpty(existingTid))
                {
                    getTransactionResult = await redeService.GetTransactionAsync(tid: existingTid);
                }
                // 2. Se não existe TID ou não conseguiu recuperar por TID, tentar por OrderId
                else
                {
                    getTransactionResult = await redeService.GetTransactionAsync(orderId: reference);
                }
                
                // Verificar se conseguiu recuperar alguma transação
                if (getTransactionResult != null && 
                    (string.IsNullOrEmpty(getTransactionResult.ReturnCode) || getTransactionResult.ReturnCode == "00"))
                {
                    // Transação recuperada com sucesso, verificar se está aprovada
                    var authorization = getTransactionResult.Authorization;
                    
                    if (authorization != null && 
                        !string.IsNullOrEmpty(authorization.Tid) &&
                        (authorization.ReturnCode == "00" || authorization.ReturnCode == "000"))
                    {
                        // Transação estava aprovada, atualizar banco
                        await SaveTransactionAsync(orderId, isShipping,
                            "APROVADO",
                            authorization.ReturnMessage ?? "Transação recuperada",
                            authorization.Tid,
                            authorization.Brand?.AuthorizationCode ?? "",
                            authorization.Nsu,
                            2);

                        result.Success = true;
                        result.Message = $"Pagamento {GetChargeTypeLabel(chargeType).ToLower()} recuperado com sucesso";
                        return result;
                    }
                    else
                    {
                        // Transação foi recuperada mas está com erro, salvar no banco e tentar nova transação
                        string tid = authorization?.Tid ?? "";
                        string returnCode = authorization?.ReturnCode ?? (getTransactionResult?.ReturnCode ?? "");
                        string nsu = authorization?.Nsu ?? "";
                        string authCode = authorization?.Brand?.AuthorizationCode ?? "";

                        // Buscar a descrição do erro na tabela tbRedeErros
                        string returnMessage = await GetErrorDescriptionAsync(returnCode) 
                            ?? authorization?.ReturnMessage 
                            ?? (getTransactionResult?.ReturnMessage ?? "Transação recuperada com erro");

                        await SaveTransactionAsync(orderId, isShipping,
                            "ERRO",
                            returnMessage,
                            tid,
                            authCode,
                            nsu,
                            3);

                        // Não retornar erro, continuar para tentar nova autorização
                        // result.Success = false;
                        // result.ErrorMessage = $"Erro ao recuperar pagamento {GetChargeTypeLabel(chargeType).ToLower()}: {returnMessage}";
                        // return result;
                    }
                }
                // Se não conseguiu recuperar nenhuma transação aprovada, continuar com nova autorização

                // Processar nova autorização (fluxo normal)
                authResult = await ProcessAuthorizationAsync(orderInfo, isShipping, isFull, redeService);

                if (authResult.Success)
                {
                    // Salvar resultado da autorização com status APROVADO
                    await SaveTransactionAsync(orderId, isShipping,
                        "APROVADO",
                        authResult.ReturnMessage ?? "",
                        authResult.Tid,
                        authResult.AuthorizationCode,
                        authResult.Nsu,
                        2);

                    result.Success = true;
                    result.Message = $"Pagamento {GetChargeTypeLabel(chargeType).ToLower()} processado com sucesso";
                }
                else
                {
                    string returnCode = authResult.ReturnCode ?? "";
                    string returnMessage = authResult.ReturnMessage ?? "";
                    string errorMessage = authResult.ErrorMessage ?? "Erro desconhecido";

                    // Se ReturnCode está vazio, tentar extrair do JSON em ErrorMessage
                    if (string.IsNullOrEmpty(returnCode) && !string.IsNullOrEmpty(errorMessage) && errorMessage.TrimStart().StartsWith("{"))
                    {
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(errorMessage);
                            if (jsonDoc.RootElement.TryGetProperty("returnCode", out var returnCodeElement))
                            {
                                returnCode = returnCodeElement.GetString() ?? "";
                            }
                            if (jsonDoc.RootElement.TryGetProperty("returnMessage", out var returnMessageElement))
                            {
                                returnMessage = returnMessageElement.GetString() ?? "";
                            }
                        }
                        catch
                        {
                            // Se falhar o parse, mantém os valores originais
                        }
                    }

                    // Buscar descrição do erro da tabela usando returnCode
                    var errorDescription = await GetErrorDescriptionAsync(returnCode);

                    // Gravar código do erro no status e descrição da tabela no campo DESC
                    await SaveTransactionErrorAsync(orderId, isShipping,
                        returnCode,
                        errorDescription ?? "",
                        1);

                    // Para a mensagem de retorno, usar a descrição da tabela se disponível, senão usar returnMessage ou errorMessage
                    var displayMessage = errorDescription;
                    if (string.IsNullOrEmpty(displayMessage))
                    {
                        displayMessage = !string.IsNullOrEmpty(returnMessage) ? returnMessage : errorMessage;
                    }

                    // Mostrar código do erro junto com a descrição
                    result.ErrorMessage = !string.IsNullOrEmpty(returnCode) 
                        ? $"Erro na autorização [{returnCode}]: {displayMessage}"
                        : $"Erro na autorização: {displayMessage}";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Erro ao processar pagamento: {ex.Message}";
            }

            return result;
        }

        private async Task<RedeAuthorizationResult> ProcessAuthorizationAsync(AcrOrderInfo orderInfo, bool isShipping, bool isFull, IRedeService redeService)
        {
            // Valor dos produtos: parcVal * parc (já gravado na conclusão da venda com crédito e desconto aplicados)
            decimal produtoAmount = orderInfo.ParcVal * orderInfo.Installments;
            
            var amount = isShipping ? orderInfo.ShippingAmount :
                        isFull ? produtoAmount + orderInfo.ShippingAmount :
                        produtoAmount;

            var installments = isShipping ? 1 : orderInfo.Installments;
            var reference = isShipping ? $"S{orderInfo.OrderId}" : orderInfo.OrderId.ToString();

            var decrypted = await _siteApi.DecryptAsync(orderInfo.CardData);
            var parts = decrypted.Split("-");
            var cardData = (Number: parts.Length > 0 ? parts[0] : "", SecurityCode: parts.Length > 1 ? parts[1] : "");
            var expiry = orderInfo.CardExpiry.Split('/');

            var request = new RedeAuthorizationRequest
            {
                Capture = true,
                Kind = "credit",
                Reference = reference,
                Amount = (int)(amount * 100),
                Installments = installments,
                CardholderName = orderInfo.CustomerName,
                CardNumber = cardData.Number,
                ExpirationMonth = int.Parse(expiry[0]),
                ExpirationYear = int.Parse(expiry[1]),
                SecurityCode = cardData.SecurityCode,
                SoftDescriptor = "",
                Subscription = false,
                Origin = 1,
                DistributorAffiliation = 0,
                BrandTid = "",
                StorageCard = "0"
            };

            return await redeService.ProcessAuthorizationAsync(request);
        }

        private async Task<RedeConfirmationResult> ProcessConfirmationAsync(string tid, int amount, IRedeService redeService)
        {
            return await redeService.ProcessConfirmationAsync(tid, amount);
        }

        private async Task SaveTransactionAsync(int orderId, bool isShipping, string status, string description, string tid, string authCode, string nsu, int step)
        {
            if (isShipping)
            {
                await SaveShippingTransactionDBAsync(orderId, status, description, tid, authCode, nsu, step);
            }
            else
            {
                await SavePaymentTransactionDBAsync(orderId, status, description, tid, authCode, nsu, step);
            }
        }

        private async Task SaveTransactionErrorAsync(int orderId, bool isShipping, string status, string description, int step)
        {
            if (isShipping)
            {
                await SaveShippingTransactionErrorDBAsync(orderId, status, description, step);
            }
            else
            {
                await SavePaymentTransactionErrorDBAsync(orderId, status, description, step);
            }
        }

        private string GetChargeTypeLabel(string chargeType)
        {
            return chargeType switch
            {
                "FRETE" => "FRETE",
                "PRODUTO+FRETE" => "PRODUTO+FRETE",
                _ => "PRODUTO"
            };
        }

        public async Task<string?> GetErrorDescriptionAsync(string errorCode)
        {
            try
            {
                return await GetErrorDescriptionDBAsync(errorCode);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao buscar descrição do erro: {ex.Message}", ex);
            }
        }

    }
}
