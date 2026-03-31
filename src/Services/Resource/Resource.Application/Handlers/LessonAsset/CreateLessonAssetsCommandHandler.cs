using Contracts.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Commands.LessonAsset;
using Resource.Application.Common.Interfaces;
using Shared.DTOs.Cloudinary;
using Shared.Helper;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.LessonAsset
{
    public class CreateLessonAssetsCommandHandler : IRequestHandler<CreateLessonAssetsCommand, CreateLessonAssetsResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<CreateLessonAssetsCommandHandler> _logger;
        
        public CreateLessonAssetsCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            ILogger<CreateLessonAssetsCommandHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }
        public async Task<CreateLessonAssetsResponse> Handle(CreateLessonAssetsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"=== CreateLessonAssetsCommandHandler START === Assets count: {request.Assets.Count}");
            
            var lesson = await _unitOfWork.Lessons.FindByIdAsync(request.Assets.First().LessonId, cancellationToken);
            if (lesson == null)
            {
                throw new KeyNotFoundException($"Lesson with ID {request.Assets.First().LessonId} not found.");
            }

            var assetsList = new List<Domain.Entities.LessonAsset>();

            foreach (var asset in request.Assets)
            {
                var extension = Path.GetExtension(asset.Name)?.ToLowerInvariant();
                _logger.LogInformation($"Processing asset: {asset.Name}, Extension: {extension}, Size: {asset.AssetBytes?.Length ?? 0} bytes");

                UploadAssetResponse uploadAssetReponse;

                if (FileTypeHelper.IsImage(asset.AssetBytes))
                {
                    _logger.LogInformation($"Detected as IMAGE: {asset.Name} - Uploading to Cloudinary");
                    // Images still use Cloudinary
                    uploadAssetReponse = await _cloudinaryService.UploadImageAsync(new UploadImageBytesRequest
                    {
                        FileBytes = asset.AssetBytes,
                        FileName = asset.Name,
                    });
                }
                else if (FileTypeHelper.IsVideo(asset.AssetBytes))
                {
                    _logger.LogInformation($"Detected as VIDEO: {asset.Name} - Uploading to R2");
                    // Videos now use R2 storage
                    uploadAssetReponse = await _cloudinaryService.UploadVideoToR2Async(new UploadVideoBytesRequest
                    {
                        FileBytes = asset.AssetBytes,
                        FileName = asset.Name
                    });
                }
                else if (FileTypeHelper.IsDocument(asset.AssetBytes))
                {
                    _logger.LogInformation($"Detected as DOCUMENT: {asset.Name} - Uploading to R2");
                    // Documents (PDF, PPTX, etc.) now use R2 storage
                    uploadAssetReponse = await _cloudinaryService.UploadDocumentToR2Async(new UploadDocumentBytesRequest
                    {
                        FileBytes = asset.AssetBytes,
                        FileName = asset.Name
                    });
                }
                else
                {
                    _logger.LogError($"UNSUPPORTED file type: {asset.Name}");
                    throw new NotSupportedException($"Unsupported file type: {asset.Name}");
                }

                _logger.LogInformation($"Upload completed for {asset.Name}: {uploadAssetReponse.AssetUrl}");

                // Tạo entity LessonAsset - use URL as-is from R2
                var lessonAsset = new Domain.Entities.LessonAsset
                {
                    LessonId = asset.LessonId,
                    //FileName = asset.Name,
                    AssetUrl = uploadAssetReponse.AssetUrl,
                    Width = uploadAssetReponse.Width,
                    Height = uploadAssetReponse.Height,
                    Duration = uploadAssetReponse.Duration,
                    Format = uploadAssetReponse.Format,
                    Size = uploadAssetReponse.Size,
                    Type = uploadAssetReponse.Type,
                    PublicId = uploadAssetReponse.PublicId,
                    CreatedAt = DateTime.UtcNow,
                    Name = asset.Name
                };

                assetsList.Add(lessonAsset);
            }

            await _unitOfWork.LessonAssets.AddRangeAsync(assetsList, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CreateLessonAssetsResponse();

            response.Assets.AddRange(assetsList.Select(asset => new CreateLessonAssetResponseModel
            {
                Id = asset.Id,
                AssetUrl = asset.AssetUrl
            }));

            return response;
        }
    }
}
