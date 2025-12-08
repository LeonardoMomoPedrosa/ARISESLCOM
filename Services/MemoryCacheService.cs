using Microsoft.Extensions.Caching.Memory;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ARISESLCOM.Services
{
    public class MemoryCacheService(IMemoryCache mCache) : BaseCacheServices, IRedisCacheService
    {
        private readonly IMemoryCache _mcache = mCache;

        public async Task SetCacheValueAsync<T>(string key, T value, TimeSpan expiration)
        {
            var json = JsonSerializer.Serialize(value);
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            _mcache.Set(key, json, cacheEntryOptions);
        }

        public async Task<T> GetCacheValueAsync<T>(string key)
        {
            var hasValue = _mcache.TryGetValue(key, out string json);
            return hasValue ? JsonSerializer.Deserialize<T>(json) : default;
        }

        public async Task DeleteCacheValueAsync(string key)
        {
            _mcache.Remove(key);
        }
    }
}
