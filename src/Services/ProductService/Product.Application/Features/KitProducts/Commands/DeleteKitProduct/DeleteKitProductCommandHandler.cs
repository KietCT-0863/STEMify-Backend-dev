using MediatR;
using Product.Application.Common.Interfaces;

namespace Product.Application.Features.KitProducts.Commands.DeleteKitProduct
{
    public class DeleteKitProductCommandHandler : IRequestHandler<DeleteKitProductCommand, bool>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public DeleteKitProductCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteKitProductCommand request, CancellationToken cancellationToken)
        {
            var kit = await _unitOfWork.KitProducts.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (kit == null)
                throw new KeyNotFoundException($"Kit with ID {request.Id} not found.");

            // Soft delete by setting status to Archived
            kit.Status = Domain.Enums.KitProductStatus.Archived;
            await _unitOfWork.KitProducts.UpdateAsync(kit, cancellationToken);
            return (await _unitOfWork.SaveChangesAsync(cancellationToken)) > 0;
        }
    }
}
