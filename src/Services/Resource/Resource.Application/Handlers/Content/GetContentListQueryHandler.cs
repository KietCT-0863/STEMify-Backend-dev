using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Content;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Content
{
    public class GetContentListQueryHandler : IRequestHandler<GetContentListQuery, ContentList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetContentListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ContentList> Handle(
            GetContentListQuery request,
            CancellationToken cancellationToken
        )
        {
            var contents = await _unitOfWork.Contents.GetAllAsync(cancellationToken);

            var list = new ContentList();
            foreach (var content in contents)
            {
                var response = new ContentResponse
                {
                    Id = content.Id,
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
                    Status = content.Status.ToString(),
                };

                list.Contents.Add(response);
            }

            return list;
        }
    }
}
