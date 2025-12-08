using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IGroupDomainModel : IDomainModel
    {
        public Task<List<ProductTypeModel>> GetProductTypeModelsAsync();
        public Task<List<ProductSubTypeModel>> GetProductSubTypeModelsAsync(int typeId);
        public Task<ActionResultModel> UpdateGroupDBAsync(ProductTypeModel model);
        public Task<ActionResultModel> DeleteGroupAsync(int pkid);
        public Task<ActionResultModel> NewGroupDBAsync(string tipo, string descricao);

        public Task<ActionResultModel> UpdateSubGroupDBAsync(ProductSubTypeModel model);
        public Task<ActionResultModel> DeleteSubGroupAsync(int typeId, int subTypeId);
        public Task<ActionResultModel> NewSubGroupDBAsync(ProductSubTypeModel model);
    }
}
