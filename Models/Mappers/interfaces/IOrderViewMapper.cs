using ARISESLCOM.Models.Entities;

namespace ARISESLCOM.Models.Mappers.interfaces
{
    public interface IOrderViewMapper
    {
        public OrderSummaryViewModel MapOrderSummary(OrderSummaryModel orderModel);

        public List<OrderSummaryViewModel> MapOrderSummaryList(List<OrderSummaryModel> orderModelList);

        public OrderViewModel MapOrderViewModel(OrderModel orderModel);
    }
}
