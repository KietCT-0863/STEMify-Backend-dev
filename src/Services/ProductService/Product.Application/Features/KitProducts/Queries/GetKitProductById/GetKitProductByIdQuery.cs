using MediatR;
using Shared.Protos.Product;

namespace Product.Application.Features.KitProducts.Queries.GetKitProductById
{
    public class GetKitProductByIdQuery : IRequest<KitDetail>
    {
        public int Id { get; set; }
    }
}
