using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Section;
using Resource.Application.Specifications.Sections;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Section
{
    public class QuerySectionsQueryHandler : IRequestHandler<QuerySectionsQuery, PagedSectionList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QuerySectionsQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedSectionList> Handle(
            QuerySectionsQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new SectionParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                Status = request.Status,
                LessonId = request.LessonId,
            };

            var pageRequest = filter.ToPageRequest();
            
            // Determine sort direction: 0 = Ascending, 1 = Descending
            bool isDescending = request.SortDirection.HasValue && request.SortDirection.Value == 1;

            Expression<Func<Domain.Entities.Section, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(filter.Search)
                    || c.Description.ToLower().Contains(filter.Search) || c.Title.ToLower().Contains(filter.Search)
                )
                && (!filter.Status.HasValue || c.Status == filter.Status.Value)
                && (!filter.LessonId.HasValue || c.LessonId == filter.LessonId.Value)
                && c.Status != Domain.Enums.SectionStatus.Deleted; // Exclude deleted sections

            Expression<Func<Domain.Entities.Section, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "orderindex" => c => c.OrderIndex,
                    _ => c => c.OrderIndex,
                };

            var paged = await _unitOfWork.Sections.GetByPageFilter(
                pageRequest,
                query =>
                    query
                        .Include(c => c.Contents)
                        .Select(section => new Models.Section.SectionResponse
                        {
                            Id = section.Id,
                            Title = section.Title,
                            Description = section.Description,
                            Duration = section.Duration,
                            Status = section.Status,
                            LessonId = section.LessonId,
                            OrderIndex = section.OrderIndex,
                            IsVisibleToStudent = section.IsVisibleToStudent,
                            ContentIds =
                                section.Contents != null
                                    ? section.Contents.Select(s => s.Id).ToList()
                                    : new List<int>()
                        }),
                sortExpression: sortExpression,
                descending: isDescending,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            return paged.ToGrpcPagedSectionList(model => model.ToGrpcSectionResponse());
        }
    }
}
