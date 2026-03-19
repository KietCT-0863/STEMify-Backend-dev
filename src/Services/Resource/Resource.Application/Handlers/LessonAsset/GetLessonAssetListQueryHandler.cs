using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.LessonAsset;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.LessonAsset
{
    public class GetLessonAssetListQueryHandler : IRequestHandler<GetLessonAssetListQuery, PagedLessonAssetList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetLessonAssetListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PagedLessonAssetList> Handle(GetLessonAssetListQuery request, CancellationToken cancellationToken)
        {
            var pageRequest = new PageRequest
            {
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
            };

            Expression<Func<Domain.Entities.LessonAsset, object>>? sortExpression = c => c.CreatedAt;

            Expression<Func<Domain.Entities.LessonAsset, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(request.Search)
                        || c.Name.ToLower().Contains(request.Search)
                    )
                    && (request.LessonId == c.LessonId)
                    && (string.IsNullOrEmpty(request.Type) || c.Type.ToLower() == request.Type.ToLower())
                    && (!request.TagId.HasValue || c.LessonAssetTags.Any(t => t.TagId == request.TagId));

            var paged = await _unitOfWork.LessonAssets.GetByPageFilter(
                    pageRequest,
                    query => query
                            .Include(c => c.LessonAssetTags),
                    sortExpression: sortExpression,
                    descending: true,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

            // Map to PagedLessonAssetList Grpc model
            var response = new PagedLessonAssetList
            {
                Items =
                {
                    paged.Items.Select(c => new LessonAssetModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        AssetUrl = c.AssetUrl,
                        Height = c.Height ?? 0,
                        Width = c.Width ?? 0,
                    }),
                },
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages
            };
            return response;
        }
    }
}
