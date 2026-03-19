using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.KitComponents.Commands;
using Product.Domain.Entities;

namespace Product.Application.Features.KitComponents.Handlers
{
    public class CreateKitComponentCommandHandler : IRequestHandler<CreateKitComponentCommand, bool>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public CreateKitComponentCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> Handle(CreateKitComponentCommand request, CancellationToken cancellationToken)
        {
            var kit = await _unitOfWork.KitProducts.FindByIdAsync(request.KitId, cancellationToken);

            // Check if the kit exists
            if (kit == null)
            {
                throw new KeyNotFoundException($"Kit with Id {request.KitId} not found.");
            }
            var kitComponentEntities = new List<KitComponent>();

            // Process each component in the request
            foreach (var componentDto in request.KitComponents)
            {
                // Check if the component exists
                var component = await _unitOfWork.Components.FindByIdAsync(componentDto.ComponentId, cancellationToken);
                if (component == null)
                {
                    throw new KeyNotFoundException($"Component with Id {componentDto.ComponentId} not found.");
                }

                // Check if the component is already associated with the kit
                var exists = await _unitOfWork.KitComponents.FindOneAsync(
                                    x => x.KitId == request.KitId && x.ComponentId == componentDto.ComponentId,
                                    cancellationToken);

                if (exists != null)
                {
                    continue; // Skip adding this component if it already exists in the kit
                }

                var kitComponent = new KitComponent
                {
                    KitId = request.KitId,
                    ComponentId = componentDto.ComponentId,
                    Quantity = componentDto.Quantity,
                    IsMainComponent = componentDto.IsMainComponent
                };

                kitComponentEntities.Add(kitComponent);
            }
            await _unitOfWork.KitComponents.AddRangeAsync(kitComponentEntities, cancellationToken);
            return (await _unitOfWork.SaveChangesAsync(cancellationToken)) > 0;
        }
    }
}
