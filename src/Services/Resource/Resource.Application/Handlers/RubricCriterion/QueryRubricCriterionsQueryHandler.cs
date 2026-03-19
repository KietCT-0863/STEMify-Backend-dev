using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.RubricCriterion;
using Resource.Application.Specifications.RubricCriterions;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.RubricCriterion
{
    public class QueryRubricCriterionsQueryHandler
        : IRequestHandler<QueryRubricCriterionsQuery, PagedRubricCriterionList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ILogger<QueryRubricCriterionsQueryHandler> _logger;

        public QueryRubricCriterionsQueryHandler(
            IResourceUnitOfWork unitOfWork,
            ILogger<QueryRubricCriterionsQueryHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedRubricCriterionList> Handle(
            QueryRubricCriterionsQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                _logger.LogTrace(
                    " Processing QueryRubricCriterions: Page={Page}, Size={Size}",
                    request.PageNumber,
                    request.PageSize
                );
                var filter = new RubricCriterionParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                    AssignmentQuestionId = request.AssignmentQuestionId,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.RubricCriterion, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.CriterionName.ToLower().Contains(filter.Search)
                    )
                    && (!filter.AssignmentQuestionId.HasValue || (c.AssignmentQuestionId == filter.AssignmentQuestionId));

                Expression<Func<Domain.Entities.RubricCriterion, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "name" => c => c.CriterionName,
                        _ => c => c.CriterionName,
                    };

                var paged = await _unitOfWork.RubricCriterions.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedRubricCriterionList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var rubricCriterion in paged.Items)
                {
                    var RubricCriterionResponse = new RubricCriterionResponse
                    {
                        Id = rubricCriterion.Id,
                        CriterionName = rubricCriterion.CriterionName,
                        Description = rubricCriterion.Description,
                        MaxPoints = (double)rubricCriterion.MaxPoints,
                        AssignmentQuestionId = rubricCriterion.AssignmentQuestionId,
                    };

                    response.Items.Add(RubricCriterionResponse);
                }

                _logger.LogTrace("QueryRubricCriterions completed successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    " QueryRubricCriterions failed: {Message} | StackTrace: {StackTrace}",
                    ex.Message,
                    ex.StackTrace
                );
                throw new ApplicationException(
                    $"An error occurred while retrieving the RubricCriterion list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
