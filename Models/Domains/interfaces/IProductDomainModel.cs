using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IProductDomainModel : IDomainModel
    {
        public Task<ActionResultModel> CreateProductAsync(ProductModel model);
        public Task<List<ProductModel>> GetProductListBySubTypeAsync(int type, int subTypeId);
        public Task<List<ProductModel>> GetProductListByTypeDBAsync(int typeId);
        public Task<List<ProductModel>> GetProductListByNameDBAsync(string name);
        public Task<List<ProductModel>> GetProductListByStockDBAsync(string pType, bool stockInd);
        public Task<ActionResultModel> UpdateProductStockDBAsync(int pkid, bool stockInd);
        public Task<ActionResultModel> UpdateProductAsync(ProductModel model);
        public Task<ActionResultModel> DeleteProductAsync(int id);
        public Task<ActionResultModel> PatchERPIdAsync(int id, int erpId, int stockMin);
        public Task<List<ProductFullTextSearchResult>> FullTextSearchAsync(string terms);
        public Task<ProductModel> GetProductDBAsync(int productId);
        public Task<ActionResultModel> UpdateImageNameAsync(int pkid, string imageFileName);
    }
}
