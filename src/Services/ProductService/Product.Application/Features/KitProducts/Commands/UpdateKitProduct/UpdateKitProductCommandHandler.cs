using Contracts.Abstractions.Services;
using MediatR;
using Product.Application.Common.Interfaces;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Commands.UpdateKitProduct
{
    public class UpdateKitProductCommandHandler : IRequestHandler<UpdateKitProductCommand, KitResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public UpdateKitProductCommandHandler(IProductUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<KitResponse> Handle(
            UpdateKitProductCommand request,
            CancellationToken cancellationToken
        )
        {
            var kit = await _unitOfWork.KitProducts.FindByIdAsync(request.Id, cancellationToken);
            if (kit == null)
                throw new KeyNotFoundException($"Kit with ID {request.Id} not found.");

            if (!string.IsNullOrEmpty(request.Name))
                kit.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                kit.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Dimensions))
                kit.Dimensions = request.Dimensions;
            if (request.Weight.HasValue)
                kit.Weight = request.Weight.Value;
            if (request.AgeRangeId.HasValue)
                kit.AgeRangeId = request.AgeRangeId.Value;
            if (request.Status.HasValue)
                kit.Status = request.Status.Value;

            if (request.Images != null && request.Images.Count > 0)
            {
                foreach (var imageDto in request.Images)
                {
                    if (imageDto?.ImageBytes == null || imageDto.ImageBytes.Length == 0)
                        continue;

                    var uploadRequest = new UploadImageBytesRequest
                    {
                        FileBytes = imageDto.ImageBytes,
                        FileName = $"{kit.Name}-{Guid.NewGuid()}",
                    };
                    var imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;

                    var newKitImage = new Domain.Entities.KitImage
                    {
                        KitId = kit.Id,
                        ImageUrl = imageUrl,
                        AltText = imageDto.AltText
                    };

                    await _unitOfWork.KitImages.AddAsync(newKitImage, cancellationToken);
                }
            }

            await _unitOfWork.KitProducts.UpdateAsync(kit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new KitResponse()
            {
                Id = kit.Id,
                Name = kit.Name,
                Description = kit.Description,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    kit.CreatedDate
                ),
                LastModifiedDate =
                    kit.LastModifiedDate != null
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            kit.LastModifiedDate.Value
                        )
                        : null,
            };

            return response;
        }
    }
}
