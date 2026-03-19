using MediatR;
using Shared.Protos.Cart;

namespace Cart.Application.Features.Carts.Queries
{
    public class GetCartQuery : IRequest<CartResponse>
    {
        public string? UserId { get; set; }
    }
}
