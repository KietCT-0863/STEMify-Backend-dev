using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.AgeRange;
using Resource.Application.Specifications.AgeRanges;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.AgeRange
{
    public class QueryAgeRangesQueryHandler
        : IRequestHandler<QueryAgeRangesQuery, PagedAgeRangeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ILogger<QueryAgeRangesQueryHandler> _logger;

        public QueryAgeRangesQueryHandler(
            IResourceUnitOfWork unitOfWork,
            ILogger<QueryAgeRangesQueryHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PagedAgeRangeList> Handle(
            QueryAgeRangesQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                _logger.LogTrace(
                    " Processing QueryAgeRanges: Page={Page}, Size={Size}",
                    request.PageNumber,
                    request.PageSize
                );
                var filter = new AgeRangeParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                    Age = request.Age,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.AgeRange, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.AgeRangeLabel.ToLower().Contains(filter.Search)
                    )
                    && (!filter.Age.HasValue || (c.MinAge <= filter.Age && c.MaxAge >= filter.Age));

                Expression<Func<Domain.Entities.AgeRange, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "age" => c => c.MinAge,
                        _ => c => c.MinAge,
                    };

                var paged = await _unitOfWork.AgeRanges.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedAgeRangeList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var AgeRange in paged.Items)
                {
                    var AgeRangeResponse = new AgeRangeResponse
                    {
                        Id = AgeRange.Id,
                        AgeRangeLabel = AgeRange.AgeRangeLabel,
                        MinAge = AgeRange.MinAge,
                        MaxAge = AgeRange.MaxAge,
                    };

                    response.Items.Add(AgeRangeResponse);
                }

                _logger.LogTrace(" QueryAgeRanges completed successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    " QueryAgeRanges failed: {Message} | StackTrace: {StackTrace}",
                    ex.Message,
                    ex.StackTrace
                );
                throw new ApplicationException(
                    $"An error occurred while retrieving the AgeRange list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
