using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.Component.Queries;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Handlers
{
    public class GetComponentByIdQueryHandler : IRequestHandler<GetComponentByIdQuery, ComponentResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        public GetComponentByIdQueryHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ComponentResponse> Handle(GetComponentByIdQuery request, CancellationToken cancellationToken)
        {
            var component = await _unitOfWork.Components
                .FindByIdAsync(request.Id, cancellationToken);
            if (component == null)
            {
                throw new KeyNotFoundException($"Component with ID {request.Id} not found.");
            }
            var response = new ComponentResponse
            {
                Id = component.Id,
                Name = component.Name,
                Description = component.Description,
                ImageUrl = component.ImageUrl
            };
            return response;
        }
    }
}
