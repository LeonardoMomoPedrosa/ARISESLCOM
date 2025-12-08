using Microsoft.Extensions.FileProviders;
using ARISESLCOM.DTO.Api.Request;

namespace ARISESLCOM.Services.interfaces
{
    public interface ISiteApiServices
    {
        Task<bool> InvalidateAsync(IEnumerable<CacheInvalidateRequest> requests);
        Task<bool> InvalidateAsync(CacheInvalidateRequest requests);
        Task<string> UploadImageToSite(int id, IFormFile file);
        Task<string> UploadDestaqueImageToSite(IFormFile file);
        Task<string> DecryptAsync(string data);
    }
}


