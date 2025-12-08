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
    [ViewComponent(Name = "CustomerProfileComponent")]
    public class CustomerProfileComponent(IDBContext iDBContext,
                                            ICustomerDomainModel customerDomainModel,
                                            IRedisCacheService redis) : BasicComponent(redis)
    {
        private readonly IDBContext _dbContext = iDBContext;
        private readonly ICustomerDomainModel _customerDomainModel = customerDomainModel;

        public async Task<IViewComponentResult> InvokeAsync(int customerId, int orderId)
        {
            var cacheKey = RedisCacheService.GetCustProfileReditKey(customerId);
            List<CustomerProfileModel> modelList = await _redis.GetCacheValueAsync<List<CustomerProfileModel>>(cacheKey);

            if (modelList != null)
            {
                return View(modelList);
            }
            await _dbContext.GetSqlConnection().OpenAsync();
            _customerDomainModel.SetContext(_dbContext);

            try
            {
                modelList = await _customerDomainModel.GetCustomerProfileAsync(customerId, orderId);
                await SetCacheAsync(cacheKey, modelList, RedisCacheService.CUSTOMER_PROFILE_MINUTES);
            }
            finally
            {
                await _dbContext.CloseAsync();
            }

            return View(modelList);
        }

    }
}
