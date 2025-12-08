using Humanizer;
using Newtonsoft.Json;
using ARISESLCOM.DTO;
using ARISESLCOM.Models.Reports;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;
using System.Net.Http.Headers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ARISESLCOM.Services
{
    public class CorreiosService(HttpClient httpClient,
                                    IRedisCacheService redisCacheService,
                                    IConfiguration configuration) : ICorreiosService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IRedisCacheService _redis = redisCacheService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<CorreiosDTO> GetCorreiosPACAsync(string cep, int peso)
        {
            cep = cep.Replace("-", "");
            var result = new CorreiosDTO();
            var authDto = await GetCorreiosAuthDTO();

            var ceporig = _configuration["Correios:ceporigem"];
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authDto.token);
            _httpClient.DefaultRequestHeaders.Add("x-format-new", "true");
            var responsePreco = await _httpClient.GetAsync(@$"https://api.correios.com.br/preco/v1/nacional/03298?cepDestino={cep}&cepOrigem={ceporig}&psObjeto={peso}");

            //var responsePrazo = await _httpClient.GetAsync(@$"https://api.correios.com.br/prazo/v1/nacional/03298?cepDestino={cep}
              //                                              &cepOrigem={ceporig}");

            var respPrecoStr = await responsePreco.Content.ReadAsStringAsync();
            //var respPrazoStr = await responsePrazo.Content.ReadAsStringAsync();
            CorreiosPrecoDTO precoDto = JsonConvert.DeserializeObject<CorreiosPrecoDTO>(respPrecoStr);
            //CorreiosPrazoDTO prazoDto = JsonConvert.DeserializeObject<CorreiosPrazoDTO>(respPrazoStr);
            result.CorreiosPrecoDTO = precoDto;
            //result.CorreiosPrazoDTO = prazoDto;

            return result;
        }

        public async Task<CorreiosRastreamentoDTO> GetRastreamentoAsync(string codigoRastreamento)
        {
            var authDto = await GetCorreiosAuthDTO();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authDto.token);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.GetAsync(@$"https://api.correios.com.br/srorastro/v1/objetos/{codigoRastreamento}?resultado=T");
            var responseStr = await response.Content.ReadAsStringAsync();
            var rastreamentoDto = JsonConvert.DeserializeObject<CorreiosRastreamentoDTO>(responseStr);

            return rastreamentoDto;
        }

        private async Task<AuthDTO> GetCorreiosAuthDTO()
        {
            AuthDTO authDto;
            var cacheKey = RedisCacheService.GetCorreiosCacheKey();
            authDto = await _redis.GetCacheValueAsync<AuthDTO>(cacheKey);

            if (authDto != null)
            {
                return authDto;
            }

            var key = _configuration["Correios:key"];
            var ccartaopostal = _configuration["Correios:cartaopostal"];

            var authHeader = new AuthenticationHeaderValue("Basic", key);
            string cartaopostal = $"{{\"numero\": \"{ccartaopostal}\"}}";
            StringContent httpContent = new(cartaopostal, System.Text.Encoding.UTF8, "application/json");
            _httpClient.BaseAddress = new Uri("https://api.correios.com.br/");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = authHeader;

            var hresp = await _httpClient.PostAsync("token/v1/autentica/cartaopostagem", httpContent).ConfigureAwait(false);
            var resp = hresp.Content.ReadAsStringAsync();
            authDto = JsonConvert.DeserializeObject<AuthDTO>(resp.Result);

            await _redis.SetCacheValueAsync(cacheKey, authDto, TimeSpan.FromMinutes(RedisCacheService.TOKEN_MINUTES));

            return authDto;

        }
    }
}
