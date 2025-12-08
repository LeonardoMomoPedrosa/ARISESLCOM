using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ARISESLCOM.Services
{
    public class RedisCacheService(IConnectionMultiplexer conex) : BaseCacheServices, IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis = conex;

         public async Task SetCacheValueAsync<T>(string key, T value, TimeSpan expiration)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiration);
        }

        public async Task<T> GetCacheValueAsync<T>(string key)
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync(key);
            return json.HasValue ? JsonSerializer.Deserialize<T>((string)json) : default;
        }

        public async Task DeleteCacheValueAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}
