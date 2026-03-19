using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Specifications;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Queries.GetKitProductById
{
    public class GetKitProductByIdQueryHandler
        : IRequestHandler<GetKitProductByIdQuery, KitDetail>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public GetKitProductByIdQueryHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<KitDetail> Handle(
            GetKitProductByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new KitByIdSpecification(request.Id);
            var kit = await _unitOfWork.KitProducts.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (kit == null)
                throw new KeyNotFoundException($"Kit with ID {request.Id} not found.");

            var response = new KitDetail()
            {
                Id = kit.Id,
                Description = kit.Description,
                Name = kit.Name,
                Status = kit.Status.ToString(),
                Dimensions = kit.Dimensions,
                Weight = (long)kit.Weight,
                TotalComponents = kit.KitComponents.Count(),
                Images = { kit.KitImages.Select(img => new KitImageDetail
                {
                    ImageUrl = img.ImageUrl,
                    AltText = img.AltText
                }) },
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

            // AddRange vào RepeatedField
            response.Components.AddRange(
                kit.KitComponents.Select(kc => new KitComponentModel
                {
                    Id = kc.Id,
                    ComponentId = kc.ComponentId,
                    Name = kc.Component.Name,
                    Quantity = kc.Quantity,
                    IsMainComponent = kc.IsMainComponent,
                    Description = kc.Component.Description,
                    ImageUrl = kc.Component.ImageUrl
                })
            );

            return response;

        }
    }
}
