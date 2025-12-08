using Microsoft.Data.SqlClient;
using ARISESLCOM.Helpers;
using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class ProductDomainModel(IRedisCacheService redis) : ProductDB(redis), IProductDomainModel
    {
        public async Task<ActionResultModel> CreateProductAsync(ProductModel model)
        {
            var outModel = await CreateProductDBAsync(model);

            return outModel;
        }

        public async Task<List<ProductModel>> GetProductListBySubTypeAsync(int type, int subTypeId)
        {
            List<ProductModel> outModel;
            if (type <= 0)
            {
                outModel = await GetProductListByTypeDBAsync(subTypeId);
            }
            else
            {
                outModel = await GetProductListBySubTypeDBAsync(subTypeId);
            }
            return outModel;
        }

        public async Task<ActionResultModel> UpdateImageNameAsync(int pkid, string imageFileName)
        {
            return await base.UpdateProductImageDBAsync(pkid, imageFileName);
        }
    }
}
