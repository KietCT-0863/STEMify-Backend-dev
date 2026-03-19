using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Category;
using Resource.Application.Specifications.Categories;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Category
{
    public class QueryCategoriesQueryHandler
        : IRequestHandler<QueryCategoriesQuery, PagedCategoryList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryCategoriesQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedCategoryList> Handle(
            QueryCategoriesQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var filter = new CategoryParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.Topic, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.Name.ToLower().Contains(filter.Search)
                    );

                Expression<Func<Domain.Entities.Topic, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "name" => c => c.Name,
                        _ => c => c.Name,
                    };

                var paged = await _unitOfWork.Topics.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedCategoryList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var Category in paged.Items)
                {
                    var CategoryResponse = new CategoryResponse
                    {
                        Id = Category.Id,
                        Name = Category.Name,
                    };

                    response.Items.Add(CategoryResponse);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the Category list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
