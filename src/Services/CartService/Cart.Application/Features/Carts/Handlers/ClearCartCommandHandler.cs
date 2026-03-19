using Cart.Application.Common.Interfaces;
using Cart.Application.Features.Carts.Commands;
using Cart.Application.Specifications;
using MediatR;
using Shared.Protos.Cart;

namespace Cart.Application.Features.Carts.Handlers
{
    public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, CartResponse>
    {
        private readonly ICartUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public ClearCartCommandHandler(
            ICartUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<CartResponse> Handle(
            ClearCartCommand request,
            CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var isAuthenticated = !string.IsNullOrEmpty(userId);

            if (isAuthenticated)
            {
                // Clear database cart
                await ClearDatabaseCartAsync(userId, cancellationToken);
            }
            else
            {
                // Clear cookie cart
                await _unitOfWork.Carts.DeleteCartInCookieAsync(userId);
            }

            return new CartResponse
            {
                UserId = userId,
                TotalPrice = 0
            };
        }

        private async Task ClearDatabaseCartAsync(string userId, CancellationToken cancellationToken)
        {
            var spec = new CartByUserIdSpecification(userId);
            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(spec, cancellationToken);

            if (cart != null)
            {
                cart.CartItems.Clear();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}