using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Components
{
    [ViewComponent(Name = "FraudComponent")]
    public class FraudComponent(IDBContext iDBContext,
                                ICustomerDomainModel customerDomainModel,
                                IRedisCacheService redis) : BasicComponent(redis)
    {
        private readonly IDBContext _dbContext = iDBContext;
        private readonly ICustomerDomainModel _customerDomainModel = customerDomainModel;

        public async Task<IViewComponentResult> InvokeAsync(int orderId)
        {
            var cacheKey = RedisCacheService.GetFraudRedisKey(orderId);

            var cachedModel = await _redis.GetCacheValueAsync<FraudModel>(cacheKey);
            if (cachedModel != null)
            {
                return View(cachedModel);
            }

            await _dbContext.GetSqlConnection().OpenAsync();
            _customerDomainModel.SetContext(_dbContext);
            FraudModel model;
            try
            {
                model = await _customerDomainModel.GetFraudModelAsync(orderId);
                await SetCacheAsync(cacheKey, model, RedisCacheService.FRAUD_CACHE_MINUTES);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(model);
        }

    }
}
