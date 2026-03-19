using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Category;
using Resource.Application.Specifications.Categories;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Category
{
    public class GetCategoryListQueryHandler : IRequestHandler<GetCategoryListQuery, CategoryList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCategoryListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryList> Handle(
            GetCategoryListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new CategoryWithIncludesSpecification();
                var categories = await _unitOfWork.Topics.GetAllAsync(spec, cancellationToken);

                var categoryList = new CategoryList();
                foreach (var category in categories)
                {
                    var response = new CategoryResponse { Id = category.Id, Name = category.Name };
                    categoryList.Categories.Add(response);
                }

                return categoryList;
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
