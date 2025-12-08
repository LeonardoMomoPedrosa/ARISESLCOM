using Azure;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.DTO.Api.Response;
using ARISESLCOM.Helpers;
using ARISESLCOM.Infrastructure.Config;
using ARISESLCOM.Services.interfaces;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace ARISESLCOM.Services
{
    public class SiteApiServices(IHttpClientFactory httpFactory,
                                 IRedisCacheService redisCacheService,
                                 IOptions<SiteApiConfig> siteConfig) : ISiteApiServices
    {
        private readonly IHttpClientFactory _httpFactory = httpFactory;
        private readonly IRedisCacheService _redis = redisCacheService;
        private readonly SiteApiConfig _siteConfig = siteConfig.Value;

        public async Task<bool> InvalidateAsync(CacheInvalidateRequest requests)
        {
            return await InvalidateAsync([requests]);
        }


        public async Task<bool> InvalidateAsync(IEnumerable<CacheInvalidateRequest> requests)
        {
            var token = await GetTokenAsync();
            var retVal = true;

            var tasks = _siteConfig.Servers.Select(async server =>
            {
                var httpClient = _httpFactory.CreateClient(Consts.SITE_CACHE_API);
                httpClient.BaseAddress = new Uri(server.BaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var body = JsonConvert.SerializeObject(requests);
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_siteConfig.InvalidateApi, content);

                if (!response.IsSuccessStatusCode)
                {
                    retVal = false;
                }
            });

            await Task.WhenAll(tasks);

            return retVal;
        }


        public async Task<string> UploadImageToSite(int id, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            string resultFileName = fileName;

            var token = await GetTokenAsync();

            // Ler o arquivo uma única vez para evitar problemas de stream
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var tasks = _siteConfig.Servers.Select(async server =>
            {
                var httpClient = _httpFactory.CreateClient(Consts.SITE_CACHE_API);
                httpClient.BaseAddress = new Uri(server.BaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var form = new MultipartFormDataContent();
                using var stream = new MemoryStream(fileBytes);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(fileContent, "file", file.FileName);
                var url = $"{_siteConfig.ImageUploadApi}/{fileName}/{id}";
                var remoteResponse = await httpClient.PostAsync(url, form).ConfigureAwait(false);

                if (!remoteResponse.IsSuccessStatusCode)
                {
                    // Se falhar em algum servidor, limpe o nome para sinalizar erro
                    resultFileName = "";
                }
            });
            await Task.WhenAll(tasks);

            return resultFileName;
        }

        public async Task<string> UploadDestaqueImageToSite(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            string resultFileName = fileName;

            var token = await GetTokenAsync();

            // Ler o arquivo uma única vez para evitar problemas de stream
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var tasks = _siteConfig.Servers.Select(async server =>
            {
                var httpClient = _httpFactory.CreateClient(Consts.SITE_CACHE_API);
                httpClient.BaseAddress = new Uri(server.BaseUrl);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var form = new MultipartFormDataContent();
                using var stream = new MemoryStream(fileBytes);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(fileContent, "file", file.FileName);

                var url = $"{_siteConfig.DestaqueImageUploadApi}/{fileName}";
                var remoteResponse = await httpClient.PostAsync(url, form).ConfigureAwait(false);

                if (!remoteResponse.IsSuccessStatusCode)
                {
                    // Se falhar em algum servidor, limpe o nome para sinalizar erro
                    resultFileName = "";
                }
            });

            await Task.WhenAll(tasks);

            return resultFileName;
        }

        private async Task<string> GetTokenAsync()
        {
            var key = BaseCacheServices.GetSiteAuthKey();
            var cached = await _redis.GetCacheValueAsync<CacheAuthResponse>(key);
            if (cached != null && !string.IsNullOrEmpty(cached.Token))
            {
                return cached.Token;
            }

            var server = _siteConfig.Servers.First(); // Pega o primeiro s� para autenticar
            var httpClient = _httpFactory.CreateClient(Consts.SITE_CACHE_API);
            httpClient.BaseAddress = new Uri(server.BaseUrl);

            var credentials = new { username = _siteConfig.Username, password = _siteConfig.Password };
            var payload = JsonConvert.SerializeObject(credentials);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_siteConfig.AuthPath, content);
            var respStr = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Auth failed: {(int)response.StatusCode} {respStr}");
            }

            var auth = JsonConvert.DeserializeObject<CacheAuthResponse>(respStr);
            var ttl = _siteConfig.TokenCacheMinutes;
            await _redis.SetCacheValueAsync(key, auth, TimeSpan.FromMinutes(ttl));
            return auth.Token;
        }

        public async Task<string> DecryptAsync(string data)
        {
            var token = await GetTokenAsync();
            var server = _siteConfig.Servers.First();
            var httpClient = _httpFactory.CreateClient(Consts.SITE_CACHE_API);
            httpClient.BaseAddress = new Uri(server.BaseUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = JsonConvert.SerializeObject(new { data });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("apicom/dataapi/process", content);
            var respStr = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Decrypt failed: {(int)response.StatusCode} {respStr}");
            }

            dynamic obj = JsonConvert.DeserializeObject(respStr);
            return (string)obj.data.processedData;
        }
    }
}


