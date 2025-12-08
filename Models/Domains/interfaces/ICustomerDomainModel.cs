using ARISESLCOM.Data;
using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface ICustomerDomainModel : IDomainModel
    {
        Task<List<CustomerAddressModel>> GetCustomerAddressModelListAsync(int customerId, int addressId);

        Task<CustomerModel> GetCustomerModelAsync(int customerId, int addressId);
        Task<List<CustomerProfileModel>> GetCustomerProfileAsync(int customerId, int orderId);
        Task<FraudModel> GetFraudModelAsync(int orderId);
        public Task<List<CustomerModel>> GetCustomerListAsync(CustomerSearchViewModel model);
        public Task<List<CustomerModel>> GetCustomerListByNameDBAsync(string name);
        public Task UpdateCustomerDBTrust(int customerId, bool trustInd);
        public Task UpdateCustomerDBDiscount(int customerId, decimal discount);
        public Task<decimal> InsertCustomerDBCredit(int customerId, decimal creditAmount);
        public Task<decimal> GetCustomerDBCredit(int customerId);
    }
}
