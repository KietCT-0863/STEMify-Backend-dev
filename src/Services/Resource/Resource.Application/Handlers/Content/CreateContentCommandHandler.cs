using MediatR;
using Resource.Application.Commands.Content;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Content
{
    public class CreateContentCommandHandler
        : IRequestHandler<CreateContentCommand, ContentResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateContentCommandHandler(
            IResourceUnitOfWork unitOfWork
        )
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ContentResponse> Handle(
            CreateContentCommand request,
            CancellationToken cancellationToken
        )
        {
            //if (request.ContentType == ContentType.Video)
            //{
            //    if (request.FileBytes != null && request.FileBytes.Length > 0)
            //    {
            //        var uploadRequest = new UploadVideoBytesRequest
            //        {
            //            FileBytes = request.FileBytes,
            //            FileName = request.FileName,
            //            ContentType = "video/mp4",
            //        };
            //        var video = await _cloudinaryService.UploadVideoAsync(uploadRequest);
            //        fileUrl = video.AssetUrl;
            //        duration = video.Duration ?? 0;
            //    }
            //}
            //else if (request.ContentType == ContentType.Document)
            //{
            //    if (request.FileBytes != null && request.FileBytes.Length > 0)
            //    {
            //        var uploadRequest = new UploadDocumentBytesRequest
            //        {
            //            FileBytes = request.FileBytes,
            //            FileName = request.FileName,
            //            ContentType = "application/pdf",
            //        };
            //        fileUrl = (await _cloudinaryService.UploadDocumentAsync(uploadRequest)).AssetUrl;
            //        duration = 3;
            //    }
            //}

            var content = new Domain.Entities.Content
            {
                Status = Domain.Enums.ContentStatus.Published,
                ContentType = (Domain.Enums.ContentType)(int)request.ContentType,
                ContentBody = request.ContentBody,
                FileName = request.FileName ?? string.Empty,
                UploadDate = DateTimeOffset.UtcNow,
                SectionId = request.SectionId,
            };

            await _unitOfWork.Contents.AddAsync(content, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ContentResponse
            {
                Id = content.Id,
                ContentType = content.ContentType.ToString(),
                Status = content.Status.ToString(),
                ContentBody = content.ContentBody,
                FileName = content.FileName,
                FileUrl = content.FileUrl,
                UploadDate = content.UploadDate.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        content.UploadDate.Value
                    )
                    : null,
                SectionId = content.SectionId,
            };

            return response;
        }
    }
}
