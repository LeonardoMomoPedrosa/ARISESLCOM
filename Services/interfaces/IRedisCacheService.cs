namespace ARISESLCOM.Services.interfaces
{
    public interface IRedisCacheService
    {
        Task SetCacheValueAsync<T>(string key, T value, TimeSpan expiration);
        Task<T> GetCacheValueAsync<T>(string key);
        Task DeleteCacheValueAsync(string key);
    }
}
