using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.Component.Commands;

namespace Product.Application.Features.Component.Handlers
{
    public class DeleteComponentCommandHandler : IRequestHandler<DeleteComponentCommand>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public DeleteComponentCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteComponentCommand request, CancellationToken cancellationToken)
        {
            var component = await _unitOfWork.Components.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (component == null)
                throw new KeyNotFoundException($"Component with ID {request.Id} not found.");

            await _unitOfWork.Components.DeleteAsync(component, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
