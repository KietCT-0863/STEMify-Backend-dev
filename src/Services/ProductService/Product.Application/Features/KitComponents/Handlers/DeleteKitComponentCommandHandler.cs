using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.KitComponents.Commands;
using Product.Domain.Entities;

namespace Product.Application.Features.KitComponents.Handlers
{
    public class DeleteKitComponentCommandHandler : IRequestHandler<DeleteKitComponentCommand, bool>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public DeleteKitComponentCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteKitComponentCommand request, CancellationToken cancellationToken)
        {
            var componentsToDelete = new List<KitComponent>();

            foreach (var id in request.Ids)
            {
                var kitComponent = await _unitOfWork.KitComponents.FindByIdAsync(id, cancellationToken);
                if (kitComponent == null)
                {
                    throw new KeyNotFoundException($"KitComponent with Id {id} not found.");
                }

                componentsToDelete.Add(kitComponent);
            }

            if (componentsToDelete.Count > 0)
            {
                await _unitOfWork.KitComponents.DeleteRangeAsync(componentsToDelete, cancellationToken);
                return (await _unitOfWork.SaveChangesAsync(cancellationToken)) > 0;
            }

            return false;
        }
    }
}
