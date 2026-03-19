using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Standard;
using Resource.Application.Specifications.Standards;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Standard
{
    public class QueryStandardsQueryHandler
        : IRequestHandler<QueryStandardsQuery, PagedStandardList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryStandardsQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedStandardList> Handle(
            QueryStandardsQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var filter = new StandardParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.Standard, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.Name.ToLower().Contains(filter.Search)
                    );

                Expression<Func<Domain.Entities.Standard, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "name" => c => c.Name,
                        _ => c => c.Name,
                    };

                var paged = await _unitOfWork.Standards.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedStandardList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var Standard in paged.Items)
                {
                    var StandardResponse = new StandardResponse
                    {
                        Id = Standard.Id,
                        StandardName = Standard.Name,
                        Description = Standard.Description
                    };

                    response.Items.Add(StandardResponse);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the Standard list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
