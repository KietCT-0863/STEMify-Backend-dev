using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Curriculum;
using Resource.Application.Specifications.Curriculums;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Curriculum
{
    public class QueryCurriculumsQueryHandler : IRequestHandler<QueryCurriculumsQuery, PagedCurriculumList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;

        public QueryCurriculumsQueryHandler(IResourceUnitOfWork unitOfWork, IUserCacheService userCache)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
        }

        public async Task<PagedCurriculumList> Handle(
            QueryCurriculumsQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new CurriculumParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                CreatedByUserId = request.CreatedByUserId,
                Status = request.Status,
                Code = request.Code
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.Curriculum, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(filter.Search)
                    || c.Title.ToLower().Contains(filter.Search)
                    || c.Description.ToLower().Contains(filter.Search)
                )
                && (filter.Status.HasValue ? c.Status == filter.Status.Value
                : (c.Status != Domain.Enums.CurriculumStatus.Deleted && c.Status != Domain.Enums.CurriculumStatus.Archived))
                && (
                    string.IsNullOrEmpty(filter.CreatedByUserId)
                    || c.CreatedByUserId == filter.CreatedByUserId
                )
                && (string.IsNullOrEmpty(filter.Code) || c.Code.ToLower().Contains(filter.Code.ToLower()));

            Expression<Func<Domain.Entities.Curriculum, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "title" => c => c.Title,
                    "createddate" => c => c.CreatedDate,
                    "lastmodifieddate" => c => c.LastModifiedDate ?? DateTime.MinValue,
                    _ => c => c.CreatedDate,
                };

            bool descending = request.SortDirection == Shared.Enums.SortDirection.Desc;

            var paged = await _unitOfWork.Curriculums.GetByPageFilter(
                pageRequest,
                query =>
                    query
                        .Select(curriculum => new Models.Curriculum.CurriculumResponse
                        {
                            Id = curriculum.Id,
                            Title = curriculum.Title,
                            Code = curriculum.Code,
                            ImageUrl = curriculum.ImageUrl,
                            Description = curriculum.Description,
                            Status = curriculum.Status,
                            CreatedByUserId = curriculum.CreatedByUserId,
                            ApprovedByUserId = curriculum.ApprovedByUserId,
                            ApprovedAt = curriculum.ApprovedAt,
                            CreatedDate = curriculum.CreatedDate,
                            LastModifiedDate = curriculum.LastModifiedDate,
                            CourseCount = curriculum.CurriculumCourses.Count,
                        }),
                sortExpression: sortExpression,
                descending: descending,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            // Fetch CreatedByUserName for each curriculum
            foreach (var curriculum in paged.Items)
            {
                var user = await _userCache.GetByIdAsync(Guid.Parse(curriculum.CreatedByUserId), cancellationToken);
                curriculum.CreatedByUserName = user?.Name ?? curriculum.CreatedByUserId;
            }

            return paged.ToGrpcPagedCurriculumList(x => x.ToGrpcCurriculumResponse());
        }
    }
}
