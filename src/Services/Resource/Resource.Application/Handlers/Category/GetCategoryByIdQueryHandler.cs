using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Category;
using Resource.Application.Specifications.Categories;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Category
{
    public class GetCategoryByIdQueryHandler
        : IRequestHandler<GetCategoryByIdQuery, CategoryResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCategoryByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryResponse> Handle(
            GetCategoryByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CategoryByIdSpecification(request.Id);
            var category = await _unitOfWork.Topics.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.Id} not found.");

            var response = new CategoryResponse() { Id = category.Id, Name = category.Name };

            return response;
        }
    }
}
