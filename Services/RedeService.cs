using ARISESLCOM.Services.interfaces;
using ARISESLCOM.DTO.Rede;
using ARISESLCOM.Infrastructure.Config;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ARISESLCOM.Services
{
    public class RedeService : IRedeService
    {
        private readonly HttpClient _httpClient;
        private readonly RedeConfig _redeConfig;
        private readonly string _username;
        private readonly string _password;
        private readonly string _endpoint;
        private readonly string _authenticationUrl;
        private readonly ILogger<RedeService> _logger;
        private readonly IRedisCacheService _redisCache;
        private static readonly TimeSpan TokenCacheTime = TimeSpan.FromMinutes(50);

        public RedeService(HttpClient httpClient, IOptions<RedeConfig> redeConfig, ILogger<RedeService> logger, IRedisCacheService redisCache)
        {
            _httpClient = httpClient;
            _redeConfig = redeConfig.Value;
            _username = _redeConfig.Username ?? "";
            _password = _redeConfig.Password ?? "";
            _endpoint = _redeConfig.Endpoint ?? "";
            _authenticationUrl = _redeConfig.AuthenticationUrl ?? "";
            _logger = logger;
            _redisCache = redisCache;
            
            _logger.LogInformation("RedeService inicializado - Endpoint: {Endpoint}, AuthUrl: {AuthUrl}, Username: {Username}", 
                _endpoint, _authenticationUrl, _username);
            
            _httpClient.BaseAddress = null;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private string GetBasicAuthCredentials()
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            return credentials;
        }

        private async Task<string?> GetOAuthTokenAsync()
        {
            const string cacheKey = "rede_oauth_token";
            
            try
            {
                // Verificar cache primeiro
                var cachedToken = await _redisCache.GetCacheValueAsync<string>(cacheKey);
                if (!string.IsNullOrEmpty(cachedToken))
                {
                    _logger.LogInformation("Token OAuth2 encontrado no cache");
                    return cachedToken;
                }

                _logger.LogInformation("Obtendo token OAuth2 da e.Rede");

                if (string.IsNullOrEmpty(_authenticationUrl))
                {
                    _logger.LogError("AuthenticationUrl não configurada");
                    return null;
                }

                // Preparar requisição de autenticação
                var basicCredentials = GetBasicAuthCredentials();
                
                // Preparar form data para OAuth2
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials")
                };
                
                var content = new FormUrlEncodedContent(formData);
                _logger.LogInformation("Enviando requisição OAuth2 com grant_type=client_credentials");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _authenticationUrl);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicCredentials);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<RedeOAuthTokenResponse>(responseContent, JsonOptions);

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        _logger.LogInformation("Token OAuth2 obtido com sucesso. Expira em: {ExpiresIn} segundos", tokenResponse.ExpiresIn);
                        
                        // Cache do token
                        await _redisCache.SetCacheValueAsync(cacheKey, tokenResponse.AccessToken, TokenCacheTime);
                        
                        return tokenResponse.AccessToken;
                    }
                    else
                    {
                        _logger.LogError("Token não encontrado na resposta da e.Rede");
                        return null;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro ao obter token OAuth2. Status: {StatusCode}, Content: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção ao obter token OAuth2: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        public async Task<RedeAuthorizationResult> ProcessAuthorizationAsync(RedeAuthorizationRequest request)
        {
            _logger.LogInformation("Processando autorização para pedido {OrderId} com valor {Amount}", 
                request.Reference, request.Amount);
            
            var result = new RedeAuthorizationResult();

            try
            {
                // Obter token OAuth2
                var token = await GetOAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Não foi possível obter token OAuth2");
                    result.Success = false;
                    result.ErrorMessage = "Falha na autenticação";
                    return result;
                }

                var jsonString = JsonSerializer.Serialize(request, JsonOptions);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpoint);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResult = JsonSerializer.Deserialize<RedeAuthorizationResult>(responseContent, JsonOptions);

                    if (authResult != null)
                    {
                        result = authResult;
                        result.Success = true;
                        _logger.LogInformation("Autorização processada com sucesso. ReturnCode: {ReturnCode}", result.ReturnCode);
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao deserializar resposta da e.Rede");
                        result.Success = false;
                        result.ErrorMessage = "Falha ao processar resposta da e.Rede";
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorCode = response.StatusCode.ToString();
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro na comunicação com e.Rede. Status: {StatusCode}, Content: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    
                    result.ErrorMessage = !string.IsNullOrEmpty(errorContent) 
                        ? errorContent 
                        : (response.ReasonPhrase ?? "Erro na comunicação com e.Rede");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção durante autorização: {ErrorMessage}", ex.Message);
                result.Success = false;
                result.ErrorCode = "EXCEPTION";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<RedeConfirmationResult> ProcessConfirmationAsync(string tid, int amount)
        {
            var result = new RedeConfirmationResult();

            try
            {
                // Obter token OAuth2
                var token = await GetOAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Não foi possível obter token OAuth2 para confirmação");
                    result.Success = false;
                    result.ErrorMessage = "Falha na autenticação";
                    return result;
                }

                var url = $"{_endpoint}/{tid}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var confirmResult = JsonSerializer.Deserialize<RedeConfirmationResult>(responseContent, JsonOptions);

                    if (confirmResult != null)
                    {
                        result = confirmResult;
                        result.Success = true;
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorCode = response.StatusCode.ToString();
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.ErrorMessage = !string.IsNullOrEmpty(errorContent) 
                        ? errorContent 
                        : (response.ReasonPhrase ?? "Erro na confirmação com e.Rede");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorCode = "EXCEPTION";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<RedeGetTransactionResponse> GetTransactionAsync(string? tid = null, string? orderId = null)
        {
            _logger.LogInformation("Buscando transação. TID: {Tid}, OrderId: {OrderId}", tid, orderId);
            
            var result = new RedeGetTransactionResponse();

            try
            {
                // Validar que pelo menos um parâmetro foi fornecido
                if (string.IsNullOrEmpty(tid) && string.IsNullOrEmpty(orderId))
                {
                    _logger.LogWarning("TID e OrderId não fornecidos");
                    result.ReturnCode = "ERROR";
                    result.ReturnMessage = "TID ou OrderId deve ser fornecido";
                    return result;
                }

                // Obter token OAuth2
                var token = await GetOAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Não foi possível obter token OAuth2 para busca de transação");
                    result.ReturnCode = "ERROR";
                    result.ReturnMessage = "Falha na autenticação";
                    return result;
                }

                // Construir URL baseado no parâmetro fornecido
                var url = !string.IsNullOrEmpty(tid) 
                    ? $"{_endpoint}/{tid}"
                    : $"{_endpoint}?reference={orderId}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                httpRequest.Headers.Add("Transaction-Response", "brand-return-opened");

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var transactionResult = JsonSerializer.Deserialize<RedeGetTransactionResponse>(responseContent, JsonOptions);

                    if (transactionResult != null)
                    {
                        result = transactionResult;
                        _logger.LogInformation("Transação encontrada. ReturnCode: {ReturnCode}", result.ReturnCode);
                    }
                    else
                    {
                        _logger.LogWarning("Falha ao deserializar resposta da e.Rede");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro na comunicação com e.Rede. Status: {StatusCode}, Content: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    
                    // Tentar fazer parse do JSON de erro
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<RedeGetTransactionResponse>(errorContent, JsonOptions);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.ReturnCode))
                        {
                            result = errorResponse;
                        }
                        else
                        {
                            result.ReturnCode = response.StatusCode.ToString();
                            result.ReturnMessage = !string.IsNullOrEmpty(errorContent)
                                ? errorContent
                                : (response.ReasonPhrase ?? "Erro ao buscar transação");
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Erro ao fazer parse da resposta de erro");
                        result.ReturnCode = response.StatusCode.ToString();
                        result.ReturnMessage = !string.IsNullOrEmpty(errorContent)
                            ? errorContent
                            : (response.ReasonPhrase ?? "Erro ao buscar transação");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção durante busca de transação: {ErrorMessage}", ex.Message);
                result.ReturnCode = "EXCEPTION";
                result.ReturnMessage = ex.Message;
            }
            
            return result;
        }
    }
}
