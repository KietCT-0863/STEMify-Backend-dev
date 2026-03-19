using Contracts.Abstractions.Services;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.Component.Commands;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Handlers
{
    public class CreateComponentCommandHandler
        : IRequestHandler<CreateComponentCommand, ComponentResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateComponentCommandHandler(IProductUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ComponentResponse> Handle(
            CreateComponentCommand request,
            CancellationToken cancellationToken
        )
        {
            string imageUrl = "";
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Name}-image",
                };
                imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            var component = new Domain.Entities.Component
            {
                Name = request.Name,
                ImageUrl = imageUrl,
                Description = request.Description,
            };

            await _unitOfWork.Components.AddAsync(component, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ComponentResponse()
            {
                Id = component.Id,
                ImageUrl = component.ImageUrl,
                Name = component.Name,
                Description = component.Description,
            };
        }
    }
}
