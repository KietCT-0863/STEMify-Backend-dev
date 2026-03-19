using MediatR;
using Resource.Application.Commands.Category;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Category
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCategoryCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _unitOfWork.Topics.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (category == null)
                throw new KeyNotFoundException($"Category with ID {request.Id} not found.");

            await _unitOfWork.Topics.DeleteAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
