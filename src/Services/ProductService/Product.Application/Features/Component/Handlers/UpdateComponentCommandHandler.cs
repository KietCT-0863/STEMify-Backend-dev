using Contracts.Abstractions.Services;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.Component.Commands;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Handlers
{
    public class UpdateComponentCommandHandler
    : IRequestHandler<UpdateComponentCommand, ComponentResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public UpdateComponentCommandHandler(IProductUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ComponentResponse> Handle(
            UpdateComponentCommand request,
            CancellationToken cancellationToken
        )
        {
            var component = await _unitOfWork.Components.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (component == null)
                throw new KeyNotFoundException($"Component with ID {request.Id} not found.");

            if (!string.IsNullOrEmpty(request.Name))
                component.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Description))
                component.Description = request.Description;

            string imageUrl = component.ImageUrl;
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Name}-image",
                };
                imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }
            component.ImageUrl = imageUrl;

            await _unitOfWork.Components.UpdateAsync(component, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ComponentResponse()
            {
                Id = component.Id,
                ImageUrl = component.ImageUrl,
                Description = component.Description,
                Name = component.Name,
            };

            return response;

        }
    }
}
