using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using ARISESLCOM.Data;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.DB
{
    public partial class DBDomain (IRedisCacheService redis)
    {
        protected IRedisCacheService _redis = redis;

        protected IDBContext? _dbContext;
        public void SetContext(IDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected async Task SetCacheAsync<T>(string cacheKey, T model, double min)
        {
            if (model != null)
            {
                await _redis.SetCacheValueAsync(cacheKey, model, TimeSpan.FromMinutes(min));
            }
        }

        protected string GetAndEqSQL(string alias, string fieldName)
        {
            return string.Format("AND {0}{1}=@{1}", alias, fieldName);
        }

        protected void ValidateStringMaxLength(string value,
                                                string fieldName,
                                                int maxLength)
        {
            
        }

        protected SqlCommand GetSqlCommand()
        {
            SqlCommand command = new("", _dbContext.GetSqlConnection())
            {
                Transaction = _dbContext.GetTrx()
            };
            return command;
        }
    }
}