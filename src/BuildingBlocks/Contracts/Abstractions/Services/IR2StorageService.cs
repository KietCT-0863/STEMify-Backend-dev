using Shared.DTOs.Storage;

namespace Contracts.Abstractions.Services
{
    public interface IR2StorageService
    {
        Task<UploadR2Response> UploadFileAsync(UploadR2Request request);
        Task<bool> DeleteFileAsync(string fileKey);
        Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60);
    }
}
