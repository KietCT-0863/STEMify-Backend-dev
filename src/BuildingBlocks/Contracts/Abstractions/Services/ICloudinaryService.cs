using Shared.DTOs.Cloudinary;

namespace Contracts.Abstractions.Services
{
    public interface ICloudinaryService
    {
        Task DeleteVideoAsync(string publicId);

        Task<UploadAssetResponse> UploadImageAsync(UploadImageBytesRequest file);

        Task<UploadAssetResponse> UploadVideoAsync(UploadVideoBytesRequest request);

        Task<UploadAssetResponse> UploadDocumentAsync(UploadDocumentBytesRequest request);
        
        // New methods for R2 storage
        Task<UploadAssetResponse> UploadVideoToR2Async(UploadVideoBytesRequest request);
        
        Task<UploadAssetResponse> UploadDocumentToR2Async(UploadDocumentBytesRequest request);
    }
}
