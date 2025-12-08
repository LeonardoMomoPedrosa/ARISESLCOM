using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Components
{
    public abstract class BasicComponent(IRedisCacheService redis) : ViewComponent
    {
        protected IRedisCacheService _redis = redis;

        protected async Task SetCacheAsync<T>(string cacheKey, T model, double min)
        {
            if (model != null)
            {
                await _redis.SetCacheValueAsync(cacheKey, model, TimeSpan.FromMinutes(min));
            }
        }
    }
}
