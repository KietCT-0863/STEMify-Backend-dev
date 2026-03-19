using Shared.DTOs.Cloudinary;

namespace Contracts.Abstractions.Services
{
    public interface ICloudinaryService
    {
        Task DeleteVideoAsync(string publicId);

        Task<UploadAssetResponse> UploadImageAsync(UploadImageBytesRequest file);

        Task<UploadAssetResponse> UploadVideoAsync(UploadVideoBytesRequest request);

        Task<UploadAssetResponse> UploadDocumentAsync(UploadDocumentBytesRequest request);
    }
}
