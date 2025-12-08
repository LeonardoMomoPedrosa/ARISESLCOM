using ARISESLCOM.Data;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Models.Reports;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IOrderDomainModel : IDomainModel
    {
        public Task<ActionResultModel> ChangeOrderStatusAsync(int orderId, string status, string origStatus);

        Task<OrderModel> GetOrderAsync(int orderId);

        Task<bool> OrderExistsAsync(int orderId);

        public Task<List<OrderSummaryModel>> GetOrdersByStatusAsync(string status);

        public Task<List<OrderSummaryModel>> SearchOrderByCustomerNameDBAsync(string customerName);

        public Task<int> UpdateTrackingAsync(TrackingModel model);

        public Task<List<TrackingModel>> GetTrackingHistoryAsync();

        public Task<List<OrderStatusReportModel>> GetOrderStatusReportDBAsync();

        public Task<List<OrderStatusTodayReportModel>> GetOrderStatusTodayReportDBAsync();

        public Task<ActionResultModel> AddProduct(int orderId, int customerId, int productId, int qtd);

        public Task<ActionResultModel> DeleteOrderDetailAsync(int orderId, int odpkid);

        public Task<ActionResultModel> ReturnCreditAsync(int orderId);

        public Task<List<OrderModel>> GetCreditCardOrdersForProcessingAsync();
        
        public Task<List<OrderSummaryModel>> GetRejectedOrdersTodayAsync();
        
        public Task<List<OrderSummaryModel>> GetSentOrdersTodayAsync();
        
        public Task<List<OrderSummaryModel>> GetCancelledOrdersTodayAsync();
    }
}
