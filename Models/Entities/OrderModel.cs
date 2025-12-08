namespace ARISESLCOM.Models.Entities
{
    public class OrderModel
    {
        public CustomerModel CustomerModel { get; set; }
        public OrderSummaryModel OrderSummary { get; set; }
        public List<OrderItemModel> Items { get; set; }
        public AirportModel AirportModel { get; set; }
        public BuslogModel BuslogModel { get; set; }
    }
}
