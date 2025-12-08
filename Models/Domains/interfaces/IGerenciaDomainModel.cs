namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IGerenciaDomainModel : IDomainModel
    {
        public Task<String> GetGerPasswordDBAsync(string name);
        public Task<string> GetBannerPromoValueDBAsync();
        public Task UpdateBannerPromoDBAsync(string value);
    }
}
