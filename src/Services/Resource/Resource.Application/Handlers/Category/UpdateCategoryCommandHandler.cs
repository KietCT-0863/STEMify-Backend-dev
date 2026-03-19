using MediatR;
using Resource.Application.Commands.Category;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Categories;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Category
{
    public class UpdateCategoryCommandHandler
        : IRequestHandler<UpdateCategoryCommand, CategoryResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateCategoryCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CategoryResponse> Handle(
            UpdateCategoryCommand request,
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

            category.Name = request.Name;

            await _unitOfWork.Topics.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CategoryResponse() { Id = category.Id, Name = category.Name };

            return response;
        }
    }
}
