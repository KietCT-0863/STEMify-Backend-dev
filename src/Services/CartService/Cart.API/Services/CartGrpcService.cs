using Cart.Application.Features.Carts.Commands;
using Cart.Application.Features.Carts.Queries;
using Grpc.Core;
using MediatR;
using Shared.Protos.Cart;

namespace Cart.API.Services
{
    public class CartGrpcService : CartService.CartServiceBase
    {
        private readonly IMediator _mediator;

        public CartGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<CartResponse> GetCart(
            GetCartRequest request,
            ServerCallContext context
        )
        {
            var command = new GetCartQuery
            {
                UserId = request.UserId,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CartResponse> UpdateCartItem(
            UpdateCartItemRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCartItemCommand
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CartResponse> RemoveFromCart(
            RemoveFromCartRequest request,
            ServerCallContext context
        )
        {
            var command = new RemoveFromCartCommand
            {
                UserId = request.UserId,
                ProductId = request.ProductId,
            };
            var result = await _mediator.Send(command);

            return result;
        }

        public override async Task<CartResponse> ClearCart(
            ClearCartRequest request,
            ServerCallContext context
        )
        {
            var query = new ClearCartCommand
            {
                UserId = request.UserId,
            };
            var result = await _mediator.Send(query);
            return result;
        }
    }
}
