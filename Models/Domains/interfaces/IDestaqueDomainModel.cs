using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IDestaqueDomainModel : IDomainModel
    {
        public Task<List<DestaqueModel>> GetDestaqueListByTipoAsync(int tipo);
        public Task<DestaqueModel> GetDestaqueAsync(int id);
        public Task<List<DestaqueModel>> GetMosaicItemsAsync();
        public Task<DestaqueModel?> GetModalEntradaAsync();
        public Task<ActionResultModel> CreateDestaqueAsync(DestaqueModel model);
        public Task<ActionResultModel> UpdateDestaqueAsync(DestaqueModel model);
        public Task<ActionResultModel> DeleteDestaqueAsync(int id);
    }
}
