using ARISESLCOM.DTO;

namespace ARISESLCOM.Services.interfaces
{
    public interface ILionServices
    {
        Task<string> CreateTrx(int orderId, int trxType);
        public Task<String> SendOrder(int orderId);
        Task<string> UpdateOrderTrack(int orderId, string trackCode, string via);
        Task<ClientInfoDTO> GetClientNameByOrderId(int orderId);
    }
}
