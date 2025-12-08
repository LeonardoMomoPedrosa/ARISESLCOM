using ARISESLCOM.DTO.Rede;

namespace ARISESLCOM.Services.interfaces
{
    public interface IRedeService
    {
        Task<RedeAuthorizationResult> ProcessAuthorizationAsync(RedeAuthorizationRequest request);
        Task<RedeConfirmationResult> ProcessConfirmationAsync(string tid, int amount);
        Task<RedeGetTransactionResponse> GetTransactionAsync(string? tid = null, string? orderId = null);
    }
}
