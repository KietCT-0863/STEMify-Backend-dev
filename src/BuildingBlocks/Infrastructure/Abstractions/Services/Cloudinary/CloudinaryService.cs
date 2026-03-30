using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Contracts.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Shared.DTOs.Cloudinary;
using Shared.DTOs.Storage;
using Shared.Helper;

namespace Infrastructure.Abstractions.Services.Cloudinary
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly IR2StorageService _r2StorageService;

        public CloudinaryService(IConfiguration configuration, IR2StorageService r2StorageService)
        {
            _cloudinary = new CloudinaryDotNet.Cloudinary(
                new Account(
                    configuration["Cloudinary:CloudName"],
                    configuration["Cloudinary:ApiKey"],
                    configuration["Cloudinary:ApiSecret"]
                )
            );
            _r2StorageService = r2StorageService;
        }

        public async Task DeleteVideoAsync(string publicId)
        {
            try
            {
                var deletionParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Video,
                };

                var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

                if (deletionResult.Result == "ok")
                {
                    Console.WriteLine("Video deleted successfully!");
                }
                else
                {
                    Console.WriteLine($"Failed to delete video. Status: {deletionResult.Result}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting video: {ex.Message}");
            }
        }

        public async Task<UploadAssetResponse> UploadImageAsync(UploadImageBytesRequest file)
        {
            using (var stream = new MemoryStream(file.FileBytes))
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Overwrite = true,
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                return new UploadAssetResponse
                {
                    AssetUrl = uploadResult.Url.ToString(),
                    Size = uploadResult.Bytes,
                    Width = uploadResult.Width,
                    Height = uploadResult.Height,
                    Format = uploadResult.Format,
                    Type = uploadResult.ResourceType,
                    PublicId = uploadResult.PublicId
                };
            }
        }

        public async Task<UploadAssetResponse> UploadDocumentAsync(UploadDocumentBytesRequest request)
        {
            try
            {
                using (var stream = new MemoryStream(request.FileBytes))
                {
                    var extension = FileTypeHelper.GetDocumentExtension(request.FileBytes);

                    var fileName = $"{Path.GetFileNameWithoutExtension(request.FileName)}{extension}";

                    stream.Position = 0;

                    var publicId = Path.GetFileNameWithoutExtension(fileName);

                    var uploadParams = new RawUploadParams
                    {
                        File = new FileDescription(fileName, stream),
                        PublicId = publicId,
                        Overwrite = true,
                        UseFilename = true,
                        UniqueFilename = false,
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    // Cloudinary returns SecureUrl (https) and Url (http)
                    var fileUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();

                    return new UploadAssetResponse
                    {
                        AssetUrl = fileUrl,
                        Format = Path.GetExtension(fileName).Trim('.'),
                        Type = "raw",
                        Size = request.FileBytes.Length,
                        PublicId = uploadResult.PublicId
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading document: {ex.Message}", ex);
            }
        }

        public async Task<UploadAssetResponse> UploadVideoAsync(UploadVideoBytesRequest request)
        {
            try
            {
                using (var stream = new MemoryStream(request.FileBytes))
                {
                    var uploadParams = new VideoUploadParams
                    {
                        File = new FileDescription(request.FileName, stream),
                        Overwrite = true,
                        PublicId = Path.GetFileNameWithoutExtension(request.FileName),
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    var duration = TimeSpan.FromSeconds(uploadResult.Duration);

                    return new UploadAssetResponse
                    {
                        AssetUrl = uploadResult.Url.ToString(),
                        Duration = (int?)TimeSpan.FromSeconds(uploadResult.Duration).TotalMinutes,
                        Format = Path.GetExtension(request.FileName).Trim('.'),
                        Type = "video",
                        Size = request.FileBytes.Length,
                        PublicId = uploadResult.PublicId
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading video: {ex.Message}");
            }
        }
    }
}

        public async Task<UploadAssetResponse> UploadVideoToR2Async(UploadVideoBytesRequest request)
        {
            try
            {
                var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".mp4" => "video/mp4",
                    ".avi" => "video/x-msvideo",
                    ".mov" => "video/quicktime",
                    ".mkv" => "video/x-matroska",
                    ".webm" => "video/webm",
                    _ => "application/octet-stream"
                };

                var r2Request = new UploadR2Request
                {
                    FileBytes = request.FileBytes,
                    FileName = $"{Guid.NewGuid()}{extension}",
                    ContentType = contentType,
                    Folder = "videos"
                };

                var r2Response = await _r2StorageService.UploadFileAsync(r2Request);

                return new UploadAssetResponse
                {
                    AssetUrl = r2Response.FileUrl,
                    Format = extension.TrimStart('.'),
                    Type = "video",
                    Size = r2Response.Size,
                    PublicId = r2Response.FileKey
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading video to R2: {ex.Message}", ex);
            }
        }

        public async Task<UploadAssetResponse> UploadDocumentToR2Async(UploadDocumentBytesRequest request)
        {
            try
            {
                var extension = FileTypeHelper.GetDocumentExtension(request.FileBytes);
                var contentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".doc" => "application/msword",
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".ppt" => "application/vnd.ms-powerpoint",
                    ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/octet-stream"
                };

                var fileName = $"{Path.GetFileNameWithoutExtension(request.FileName)}-{Guid.NewGuid()}{extension}";

                var r2Request = new UploadR2Request
                {
                    FileBytes = request.FileBytes,
                    FileName = fileName,
                    ContentType = contentType,
                    Folder = "documents"
                };

                var r2Response = await _r2StorageService.UploadFileAsync(r2Request);

                return new UploadAssetResponse
                {
                    AssetUrl = r2Response.FileUrl,
                    Format = extension.TrimStart('.'),
                    Type = "document",
                    Size = r2Response.Size,
                    PublicId = r2Response.FileKey
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading document to R2: {ex.Message}", ex);
            }
        }
    }
}
