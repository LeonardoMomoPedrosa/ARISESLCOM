using ARISESLCOM.Data;
using ARISESLCOM.Models;
using ARISESLCOM.Services.interfaces;

namespace ARISESLCOM.Models.Domains.interfaces
{
    public interface IAcrDomainModel
    {
        void SetContext(IDBContext dbContext);
        Task<List<AcrOrderViewModel>> GetCreditCardOrdersForProcessingAsync();
        Task<AcrOrderInfo> GetOrderInfoAsync(int orderId);
        Task<AcrProcessResult> ProcessPaymentAsync(int orderId, string chargeType, IRedeService redeService);
        Task<string?> GetErrorDescriptionAsync(string errorCode);
    }
}

