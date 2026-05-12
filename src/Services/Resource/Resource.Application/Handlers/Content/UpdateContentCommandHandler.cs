using MediatR;
using Resource.Application.Commands.Content;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Content
{
    public class UpdateContentCommandHandler
        : IRequestHandler<UpdateContentCommand, ContentResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateContentCommandHandler(
            IResourceUnitOfWork unitOfWork
        )
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ContentResponse> Handle(
            UpdateContentCommand request,
            CancellationToken cancellationToken
        )
        {
            string fileUrl = "";
            int duration = 0;
            //if (request.ContentType == ContentType.Video)
            //{
            //    if (request.FileBytes != null && request.FileBytes.Length > 0)
            //    {
            //        var uploadRequest = new UploadVideoBytesRequest
            //        {
            //            FileBytes = request.FileBytes,
            //            FileName = request.FileName ?? "Video",
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
            //            FileName = request.FileName ?? "Document",
            //            ContentType = "application/pdf",
            //        };
            //        fileUrl = (await _cloudinaryService.UploadDocumentAsync(uploadRequest)).AssetUrl;
            //        duration = 3;
            //    }
            //}
            //if (request.ContentType == ContentType.Text)
            //{
            //    fileUrl = "";
            //}

            var content = await _unitOfWork.Contents.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (content == null)
                throw new KeyNotFoundException($"Content with ID {request.Id} not found.");

            //content.Status = request.Status;
            if (request.Status != null)
                content.Status = request.Status.Value;
            if (!string.IsNullOrEmpty(request.ContentName))
                content.ContentBody = request.ContentName;
            if (!string.IsNullOrEmpty(request.FileName))
                content.FileName = request.FileName ?? string.Empty;
            content.FileUrl = fileUrl;

            await _unitOfWork.Contents.UpdateAsync(content, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ContentResponse
            {
                Id = content.Id,
                Status = content.Status.ToString(),
                ContentBody = content.ContentBody,
                ContentType = content.ContentType.ToString(),
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
