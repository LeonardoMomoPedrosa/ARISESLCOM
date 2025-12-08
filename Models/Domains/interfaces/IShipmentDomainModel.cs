namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IShipmentDomainModel : IDomainModel
    {
        public Task<ShipmentModel> GetShipmentTranspAsync(int orderId);
        public Task<ShipmentModel> GetShipmentAirportAsync(int orderId);
        public Task<ShipmentModel> GetShipmentCorreiosAsync(int orderId);
        public Task<ShipmentModel> GetShipmentBuslogAsync(int orderId);
    }
}
