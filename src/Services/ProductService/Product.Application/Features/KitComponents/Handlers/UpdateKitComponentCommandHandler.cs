using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.KitComponents.Commands;
using Shared.Protos.Product;

namespace Product.Application.Features.KitComponents.Handlers
{
    public class UpdateKitComponentCommandHandler : IRequestHandler<UpdateKitComponentCommand, bool>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public UpdateKitComponentCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> Handle(UpdateKitComponentCommand request, CancellationToken cancellationToken)
        {
            var responses = new List<KitComponentResponse>();

            foreach (var item in request.KitComponents)
            {
                var kitComponent = await _unitOfWork.KitComponents.FindByIdAsync(item.Id, cancellationToken);
                if (kitComponent == null)
                {
                    throw new KeyNotFoundException($"KitComponent with Id {item.Id} not found.");
                }

                if (item.Quantity.HasValue)
                {
                    kitComponent.Quantity = item.Quantity.Value;
                }

                if (item.IsMainComponent.HasValue)
                {
                    kitComponent.IsMainComponent = item.IsMainComponent.Value;
                }

                await _unitOfWork.KitComponents.UpdateAsync(kitComponent); // Nếu UpdateAsync là optional, có thể bỏ qua
                responses.Add(new KitComponentResponse
                {
                    Id = kitComponent.Id,
                    KitId = kitComponent.KitId,
                    ComponentId = kitComponent.ComponentId,
                    Quantity = kitComponent.Quantity,
                    IsMainComponent = kitComponent.IsMainComponent
                });
            }

            return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
