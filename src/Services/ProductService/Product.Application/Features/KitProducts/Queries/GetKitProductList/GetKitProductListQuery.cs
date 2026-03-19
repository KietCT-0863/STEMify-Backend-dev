using MediatR;
using Product.Domain.Enums;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Queries.GetKitProductList
{
    public class GetKitProductListQuery : IRequest<PagedKitList>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public int? AgeRangeId { get; set; }
        public bool? IsPreOrder { get; set; }
        public KitProductStatus? Status { get; set; }
    }
}
