using MediatR;
using Product.Application.Models;
using Product.Domain.Enums;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Commands.UpdateKitProduct
{
    public class UpdateKitProductCommand : IRequest<KitResponse>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? StockQuantity { get; set; }
        public long? Weight { get; set; }
        public string? Description { get; set; }
        public string? Dimensions { get; set; }
        public bool? IsPreOrder { get; set; }
        public int? AgeRangeId { get; set; }
        public decimal? Price { get; set; }
        public List<KitImageUploadDto> Images { get; set; } = new();
        public KitProductStatus? Status { get; set; }
    }
}
