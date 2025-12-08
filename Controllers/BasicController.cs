using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ARISESLCOM.Controllers
{
    public class BasicController(IRedisCacheService redis) : Controller
    {
        protected IRedisCacheService _redis = redis;
        protected bool cacheSuccessInd;

        protected async Task SetCacheAsync<T>(string cacheKey, T model, double min)
        {
            if (model != null)
            {
                await _redis.SetCacheValueAsync<T>(cacheKey, model, TimeSpan.FromMinutes(min));
            }
        }
    }

   
}
