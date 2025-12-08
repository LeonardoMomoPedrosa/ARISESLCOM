using ARISESLCOM.Services.interfaces;
using ARISESLCOM.DTO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ARISESLCOM.Services
{
    public class LionServices(IConfiguration config, ILogger<LionServices> logger) : BaseServices, ILionServices
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<LionServices> _logger = logger;

        public const int TRX_EMAIL = 2;
        public const int TRX_CLIENTE_RETIRA = 3;
        public const int TRX_NAO_AUTORIZADO = 4;
        public async Task<String> SendOrder(int orderId)
        {
            using HttpClient httpClient = new();

            httpClient.BaseAddress = new Uri(GetBaseAddress());
            var response = await httpClient.GetAsync($"ajax/OrderStatusAjaxHandler.ashx?order_id={orderId}&action=send_order");

            var str = await response.Content.ReadAsStringAsync();

            return str;
        }

        public async Task<String> CreateTrx(int orderId, int trxType)
        {
            using HttpClient httpClient = new();

            httpClient.BaseAddress = new Uri(GetBaseAddress());
            var response = await httpClient.GetAsync($"ajax/OrderStatusAjaxHandler.ashx?oid={orderId}&trx_tp={trxType}&action=create_trx");

            var str = await response.Content.ReadAsStringAsync();

            return str;
        }

        public async Task<string> UpdateOrderTrack(int orderId, string trackCode, string via)
        {
            _logger.LogInformation("UpdateOrderTrack chamado - OrderId: {OrderId}, TrackCode: {TrackCode}, Via: {Via}", 
                orderId, trackCode, via);
            
            using HttpClient httpClient = new();

            var baseAddress = GetBaseAddress();
            _logger.LogInformation("BaseAddress: {BaseAddress}", baseAddress);
            
            var soapEndpoint = new Uri(new Uri(baseAddress), "ws/SalesService.asmx");
            _logger.LogInformation("SOAP Endpoint: {SoapEndpoint}", soapEndpoint);

            // Valida��o de par�metros
            var safeTrackCode = string.IsNullOrWhiteSpace(trackCode) ? string.Empty : trackCode.Trim();
            var safeVia = string.IsNullOrWhiteSpace(via) ? string.Empty : via.Trim();
            
            _logger.LogInformation("Par�metros validados - OrderId: {OrderId}, TrackCode: '{TrackCode}', Via: '{Via}'", 
                orderId, safeTrackCode, safeVia);
            
            var soapEnvelope = BuildSoapEnvelope(orderId, safeTrackCode, safeVia);
            _logger.LogInformation("SOAP Envelope completo: {SoapEnvelope}", soapEnvelope);
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.ContentType = new MediaTypeHeaderValue("text/xml")
            {
                CharSet = "utf-8"
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "\"http://tempuri.org/updateOrderTrack\"");

            var soapAction = httpClient.DefaultRequestHeaders.TryGetValues("SOAPAction", out var values) 
                ? values.FirstOrDefault() 
                : "n�o encontrado";
            
            _logger.LogInformation("Enviando requisi��o SOAP para OrderId: {OrderId}", orderId);
            _logger.LogInformation("Headers - Content-Type: {ContentType}, SOAPAction: {SOAPAction}", 
                content.Headers.ContentType?.ToString(), 
                soapAction);
            
            var response = await httpClient.PostAsync(soapEndpoint, content);
            _logger.LogInformation("Resposta recebida - Status: {StatusCode}", response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Content (primeiros 500 chars): {ResponseContent}", 
                responseContent?.Substring(0, Math.Min(500, responseContent?.Length ?? 0)));

            return responseContent;
        }

        private string BuildSoapEnvelope(int orderId, string trackCode, string via)
        {
            // Construir XML no formato exato do Postman
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine();
            xml.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            xml.AppendLine();
            xml.AppendLine("  <soap:Body>");
            xml.AppendLine();
            xml.AppendLine("    <tns:updateOrderTrack xmlns:tns=\"http://tempuri.org/\">");
            xml.AppendLine();
            xml.AppendLine($"      <tns:orderId>{orderId}</tns:orderId>");
            xml.AppendLine();
            xml.AppendLine($"      <tns:track>{XmlEscape(trackCode ?? string.Empty)}</tns:track>");
            xml.AppendLine();
            xml.AppendLine($"      <tns:via>{XmlEscape(via ?? string.Empty)}</tns:via>");
            xml.AppendLine();
            xml.AppendLine("    </tns:updateOrderTrack>");
            xml.AppendLine();
            xml.AppendLine("  </soap:Body>");
            xml.AppendLine();
            xml.AppendLine("</soap:Envelope>");

            return xml.ToString();
        }

        private string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        public async Task<ClientInfoDTO> GetClientNameByOrderId(int orderId)
        {
            _logger.LogInformation("GetClientNameByOrderId chamado - OrderId: {OrderId}", orderId);
            
            using HttpClient httpClient = new();

            var baseAddress = GetBaseAddress();
            _logger.LogInformation("BaseAddress: {BaseAddress}", baseAddress);
            
            var soapEndpoint = new Uri(new Uri(baseAddress), "ws/SalesService.asmx");
            _logger.LogInformation("SOAP Endpoint: {SoapEndpoint}", soapEndpoint);

            var soapEnvelope = BuildGetClientNameSoapEnvelope(orderId);
            _logger.LogInformation("SOAP Envelope completo: {SoapEnvelope}", soapEnvelope);
            
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.ContentType = new MediaTypeHeaderValue("text/xml")
            {
                CharSet = "utf-8"
            };

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "\"http://tempuri.org/GetClientByOrder\"");

            var soapAction = httpClient.DefaultRequestHeaders.TryGetValues("SOAPAction", out var values) 
                ? values.FirstOrDefault() 
                : "n�o encontrado";
            
            _logger.LogInformation("Enviando requisi��o SOAP para OrderId: {OrderId}", orderId);
            _logger.LogInformation("Headers - Content-Type: {ContentType}, SOAPAction: {SOAPAction}", 
                content.Headers.ContentType?.ToString(), 
                soapAction);
            
            var response = await httpClient.PostAsync(soapEndpoint, content);
            _logger.LogInformation("Resposta recebida - Status: {StatusCode}", response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Content completo: {ResponseContent}", responseContent);

            // Parse da resposta SOAP para extrair o JSON ou dados
            var clientInfo = ParseClientInfoResponse(responseContent);
            
            _logger.LogInformation("ClientInfo extra�do - ClientName: '{ClientName}', Email: '{Email}'", 
                clientInfo.ClientName, clientInfo.Email);

            return clientInfo;
        }

        private string BuildGetClientNameSoapEnvelope(int orderId)
        {
            // Construir XML no formato do Postman
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine();
            xml.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            xml.AppendLine();
            xml.AppendLine("  <soap:Body>");
            xml.AppendLine();
            xml.AppendLine("    <tns:GetClientByOrder xmlns:tns=\"http://tempuri.org/\">");
            xml.AppendLine();
            xml.AppendLine($"      <tns:orderId>{orderId}</tns:orderId>");
            xml.AppendLine();
            xml.AppendLine("    </tns:GetClientByOrder>");
            xml.AppendLine();
            xml.AppendLine("  </soap:Body>");
            xml.AppendLine();
            xml.AppendLine("</soap:Envelope>");

            return xml.ToString();
        }

        private ClientInfoDTO ParseClientInfoResponse(string soapResponse)
        {
            var result = new ClientInfoDTO { ClientName = string.Empty, Email = string.Empty };

            try
            {
                // Tentar extrair JSON do envelope SOAP
                // O JSON geralmente vem dentro de um elemento do envelope SOAP
                var jsonMatch = Regex.Match(soapResponse, @"\{[^}]*""ClientName""[^}]*""Email""[^}]*\}", RegexOptions.Singleline);
                
                if (jsonMatch.Success)
                {
                    var jsonString = jsonMatch.Value;
                    _logger.LogDebug("JSON encontrado na resposta: {Json}", jsonString);
                    
                    try
                    {
                        var clientInfo = JsonConvert.DeserializeObject<ClientInfoDTO>(jsonString);
                        if (clientInfo != null)
                        {
                            result.ClientName = clientInfo.ClientName ?? string.Empty;
                            result.Email = clientInfo.Email ?? string.Empty;
                            return result;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Erro ao deserializar JSON, tentando parse manual");
                    }
                }

                // Se n�o encontrou JSON, tentar extrair diretamente do XML
                // Procurar por elementos XML que possam conter os dados
                var clientNameMatch = Regex.Match(soapResponse, @"<(?:\w+:)?ClientName[^>]*>(.*?)</(?:\w+:)?ClientName>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var emailMatch = Regex.Match(soapResponse, @"<(?:\w+:)?Email[^>]*>(.*?)</(?:\w+:)?Email>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (clientNameMatch.Success)
                {
                    result.ClientName = clientNameMatch.Groups[1].Value.Trim();
                }

                if (emailMatch.Success)
                {
                    result.Email = emailMatch.Groups[1].Value.Trim();
                }

                // Se ainda n�o encontrou, tentar extrair texto entre tags GetClientByOrderResult
                if (string.IsNullOrEmpty(result.ClientName) && string.IsNullOrEmpty(result.Email))
                {
                    var resultMatch = Regex.Match(soapResponse, @"<(?:\w+:)?GetClientByOrderResult[^>]*>(.*?)</(?:\w+:)?GetClientByOrderResult>", 
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    
                    if (resultMatch.Success)
                    {
                        var innerContent = resultMatch.Groups[1].Value.Trim();
                        _logger.LogDebug("Conte�do interno encontrado: {Content}", innerContent);
                        
                        // Tentar deserializar como JSON
                        try
                        {
                            var clientInfo = JsonConvert.DeserializeObject<ClientInfoDTO>(innerContent);
                            if (clientInfo != null)
                            {
                                result.ClientName = clientInfo.ClientName ?? string.Empty;
                                result.Email = clientInfo.Email ?? string.Empty;
                                return result;
                            }
                        }
                        catch (JsonException)
                        {
                            // Se n�o for JSON v�lido, j� tentamos extrair do XML acima
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer parse da resposta SOAP");
            }

            return result;
        }

        private string GetBaseAddress()
        {
            return _config.GetSection("ENDPOINTS").GetValue<string>("LION");
        }
    }

    
}
