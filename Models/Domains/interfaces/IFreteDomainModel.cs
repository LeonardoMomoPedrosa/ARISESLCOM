using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IFreteDomainModel : IDomainModel
    {
        public Task<AirportModel> GetAirPortAsync(int airportId);

        public Task<BuslogModel> GetBuslogAsync(int buslogId);

        public Task<OrderModel> GetFreteInfo(OrderModel orderModel);
    }
}