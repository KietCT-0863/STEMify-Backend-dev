using Contracts.Abstractions.Services;
using MediatR;
using Product.Application.Common.Interfaces;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Commands.CreateKitProduct
{
    public class CreateKitProductCommandHandler : IRequestHandler<CreateKitProductCommand, KitResponse>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateKitProductCommandHandler(IProductUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<KitResponse> Handle(CreateKitProductCommand request, CancellationToken cancellationToken)
        {
            var kit = new Domain.Entities.KitProduct
            {
                Name = request.Name,
                Description = request.Description,
                AgeRangeId = request.AgeRangeId,
                Dimensions = request.Dimensions,
                CreatedByUserId = Guid.Parse(request.CreatedByUserId),
                Weight = request.Weight,
            };

            kit.KitImages = await UploadKitImagesAsync(request, kit.Id, cancellationToken);

            await _unitOfWork.KitProducts.AddAsync(kit, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new KitResponse()
            {
                Id = kit.Id,
                Name = kit.Name,
                Description = kit.Description,
                TotalComponents = kit.KitComponents.Count(),
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

        }

        private async Task<List<Domain.Entities.KitImage>> UploadKitImagesAsync(
            CreateKitProductCommand request,
            int kitId,
            CancellationToken cancellationToken)
        {
            var kitImages = new List<Domain.Entities.KitImage>();

            if (request.Images == null || request.Images.Count == 0)
                return kitImages;

            foreach (var imageDto in request.Images)
            {
                if (imageDto?.ImageBytes == null || imageDto.ImageBytes.Length == 0)
                    continue;

                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = imageDto.ImageBytes,
                    FileName = $"{request.Name}-{Guid.NewGuid()}",
                };

                var imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;

                kitImages.Add(new Domain.Entities.KitImage
                {
                    KitId = kitId,
                    ImageUrl = imageUrl,
                    AltText = imageDto.AltText
                });
            }

            return kitImages;
        }


    }
}
