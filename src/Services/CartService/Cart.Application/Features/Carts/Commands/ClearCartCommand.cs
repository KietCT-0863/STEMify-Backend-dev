using MediatR;
using Shared.Protos.Cart;

namespace Cart.Application.Features.Carts.Commands
{
    public class ClearCartCommand : IRequest<CartResponse>
    {
        public string? UserId { get; set; }
    }
}
