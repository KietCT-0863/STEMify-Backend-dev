using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Product.Application.Features.KitProducts.Commands.CreateKitProduct;
using Product.Application.Features.KitProducts.Commands.DeleteKitProduct;
using Product.Application.Features.KitProducts.Commands.UpdateKitProduct;
using Product.Application.Features.KitProducts.Queries.GetKitProductById;
using Product.Application.Features.KitProducts.Queries.GetKitProductList;
using Product.Application.Models;
using Shared.Enums;
using Shared.Extensions;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class KitProductGrpcService : GrpcKitProductService.GrpcKitProductServiceBase
    {
        private readonly IMediator _mediator;

        public KitProductGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<KitResponse> CreateKit(
            CreateKitRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateKitProductCommand
            {
                Name = request.Name,
                Description = request.Description,
                AgeRangeId = request.AgeRangeId,
                Weight = request.Weight,
                CreatedByUserId = request.CreatedByUserId,
                Dimensions = request.Dimensions,
                Images = request.Images.Select(img => new KitImageUploadDto
                {
                    ImageBytes = img.ImageUrl.ToByteArray(),
                    AltText = img.AltText
                }).ToList(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<KitDetail> GetKit(
            GetKitRequest request,
            ServerCallContext context
        )
        {
            var query = new GetKitProductByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }
        // This method updates kit details including images
        public override async Task<KitResponse> UpdateKit(
            UpdateKitRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateKitProductCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                Dimensions = request.Dimensions,
                Weight = request.Weight,
                AgeRangeId = request.AgeRangeId,
                Status = request.Status.ToEnumOrNull<Domain.Enums.KitProductStatus>(),
                Images = request.Images.Select(img => new KitImageUploadDto
                {
                    ImageBytes = img.ImageUrl.ToByteArray(),
                    AltText = img.AltText
                }).ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteKit(
            DeleteKitRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteKitProductCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        // This method supports filtering, sorting, and pagination
        public override async Task<PagedKitList> QueryKits(
            QueryKitsRequest request,
            ServerCallContext context
        )
        {
            var query = new GetKitProductListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                AgeRangeId = request.AgeRangeId,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                Status = request.Status != null ? System.Enum.Parse<Domain.Enums.KitProductStatus>(request.Status) : null,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}
