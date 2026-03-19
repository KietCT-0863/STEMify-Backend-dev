using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.CourseLearningOutcome;
using Resource.Application.Specifications.CourseLearningOutcomes;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class QueryCourseLearningOutcomesQueryHandler
        : IRequestHandler<QueryCourseLearningOutcomesQuery, PagedCourseLearningOutcomeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryCourseLearningOutcomesQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedCourseLearningOutcomeList> Handle(
            QueryCourseLearningOutcomesQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new CourseLearningOutcomeParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                CourseId = request.CourseId,
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.CourseLearningOutcome, bool>> predicate = c =>
                (
                    (string.IsNullOrEmpty(filter.Search) || c.Name.ToLower().Contains(filter.Search))
                    && (!filter.CourseId.HasValue || c.CourseId == filter.CourseId.Value)
                );

            Expression<Func<Domain.Entities.CourseLearningOutcome, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    _ => c => c.Name,
                };

            var paged = await _unitOfWork.CourseLearningOutcomes.GetByPageFilter(
                pageRequest,
                sortExpression: sortExpression,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new PagedCourseLearningOutcomeList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var programLearningOutcome in paged.Items)
            {
                var courseLearningOutcomeResponse = new CourseLearningOutcomeResponse
                {
                    Id = programLearningOutcome.Id,
                    Name = programLearningOutcome.Name,
                    Description = programLearningOutcome.Description,
                    CourseId = programLearningOutcome.CourseId,
                };

                response.Items.Add(courseLearningOutcomeResponse);
            }

            return response;
        }
    }
}
