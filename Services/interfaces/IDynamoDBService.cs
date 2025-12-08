using ARISESLCOM.DTO;

namespace ARISESLCOM.Services.interfaces
{
    public interface IDynamoDBService
    {
        Task<List<TrackerPedidoViewModel>> GetAllTrackingPedidosAsync();
    }
}

