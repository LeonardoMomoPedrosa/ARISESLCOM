using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface ICreditDomainModel : IDomainModel
    {
        public Task<CreditModel> GetCreditByOrderAsync(int orderId);

        public Task<ActionResultModel> DeleteCreditByOrderAsync(int orderId);
    }
}