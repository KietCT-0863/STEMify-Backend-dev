using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.LessonAsset;
using Resource.Application.Specifications.Lessons;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.LessonAsset
{
    public class GetLessonAssetByIdQueryHandler : IRequestHandler<GetLessonAssetByIdQuery, LessonAssetResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        public GetLessonAssetByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<LessonAssetResponse> Handle(GetLessonAssetByIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new LessonAssetByIdSpecification(request.Id);
            var lessonAsset = await _unitOfWork.LessonAssets.FirstOrDefaultAsync(spec, cancellationToken);

            if (lessonAsset == null)
                throw new KeyNotFoundException($"LessonAsset with ID {request.Id} not found.");

            var response = new LessonAssetResponse
            {
                Id = lessonAsset.Id,
                Name = lessonAsset.Name,
                AssetUrl = lessonAsset.AssetUrl,
                PublicId = lessonAsset.PublicId,
                Size = lessonAsset.Size,
                Type = lessonAsset.Type,
                LessonId = lessonAsset.LessonId,
                Duration = lessonAsset.Duration,
                Format = lessonAsset.Format,
                Height = lessonAsset.Height,
                Width = lessonAsset.Width,
                Tags = { lessonAsset.LessonAssetTags.Select(t => t.Tag.Name) },
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(lessonAsset.CreatedAt),
            };
            return response;
        }
    }
}
