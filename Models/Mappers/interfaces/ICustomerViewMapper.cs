using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers.interfaces
{
    public interface ICustomerViewMapper
    {
        public CustomerAddressViewModel MapCustomerAddressViewModel(CustomerAddressModel model);
    }
}
