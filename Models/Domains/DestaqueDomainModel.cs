using ARISESLCOM.Models.Domains.DB;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains
{
    public class DestaqueDomainModel(IRedisCacheService redis) : DestaqueDB(redis), IDestaqueDomainModel
    {
        public async Task<List<DestaqueModel>> GetDestaqueListByTipoAsync(int tipo)
        {
            return await GetDestaqueListByTipoDBAsync(tipo);
        }

        public async Task<DestaqueModel> GetDestaqueAsync(int id)
        {
            return await base.GetDestaqueDBAsync(id);
        }

        public async Task<List<DestaqueModel>> GetMosaicItemsAsync()
        {
            return await GetMosaicItemsDBAsync();
        }

        public async Task<DestaqueModel?> GetModalEntradaAsync()
        {
            return await GetModalEntradaDBAsync();
        }

        public async Task<ActionResultModel> CreateDestaqueAsync(DestaqueModel model)
        {
            return await CreateDestaqueDBAsync(model);
        }

        public async Task<ActionResultModel> UpdateDestaqueAsync(DestaqueModel model)
        {
            return await UpdateDestaqueDBAsync(model);
        }

        public async Task<ActionResultModel> DeleteDestaqueAsync(int id)
        {
            return await DeleteDestaqueDBAsync(id);
        }
    }
}
