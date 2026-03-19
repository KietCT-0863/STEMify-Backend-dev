using Cart.Application.Common.Interfaces;
using Cart.Application.Common.Interfaces.Cache;
using Cart.Application.Features.Carts.Commands;
using Cart.Application.Features.Carts.Queries;
using Cart.Application.Models;
using Cart.Application.Specifications;
using MediatR;
using Shared.Protos.Cart;
using Shared.Protos.Product;

namespace Cart.Application.Features.Carts.Handlers
{
    public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, CartResponse>
    {
        private readonly ICartUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IProductCacheService _productCacheService;

        public RemoveFromCartCommandHandler(
            ICartUnitOfWork unitOfWork,
            IProductCacheService productCacheService,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _productCacheService = productCacheService;
        }

        public async Task<CartResponse> Handle(
            RemoveFromCartCommand request,
            CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var isAuthenticated = !string.IsNullOrEmpty(userId);

            if (isAuthenticated)
            {
                // Remove from database cart
                await RemoveFromDatabaseCartAsync(userId, request.ProductId, cancellationToken);
                var getCartQuery = new GetCartQuery { UserId = request.UserId };
                return await _mediator.Send(getCartQuery, cancellationToken);
            }
            else
            {
                // Remove from cookie cart
                var updatedCartItems = await _unitOfWork.Carts.RemoveItemAndGetUpdatedCartAsync(
                    request.ProductId,
                    userId);

                return await BuildCookieCartResponseFromMemory(updatedCartItems, cancellationToken);
            }
        }

        private async Task<CartResponse> BuildCookieCartResponseFromMemory(
            List<CartItemDTO> cartItems,
            CancellationToken cancellationToken)
        {
            if (!cartItems.Any())
            {
                return new CartResponse
                {
                    UserId = null,
                    TotalPrice = 0
                };
            }

            var productIds = cartItems.Select(ci => ci.ItemId).ToList();
            var products = new List<ProductResponse>();
            foreach (var productId in productIds)
            {
                var product = await _productCacheService.GetByIdAsync(productId, cancellationToken);
                if (product != null)
                    products.Add(product);
            }

            var cartItemResponses = new List<CartItemResponse>();
            double totalPrice = 0;

            foreach (var cartItem in cartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ItemId);
                if (product != null)
                {
                    var subtotal = product.Price * cartItem.Quantity;
                    totalPrice += subtotal;

                    cartItemResponses.Add(new CartItemResponse
                    {
                        ProductId = cartItem.ItemId,
                        Name = product.Name,
                        Description = product.Description ?? string.Empty,
                        ImageUrl = product.ImageUrl ?? string.Empty,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    });
                }
            }

            return new CartResponse
            {
                UserId = null,
                Items = { cartItemResponses },
                TotalPrice = totalPrice
            };
        }

        private async Task RemoveFromDatabaseCartAsync(string userId, int productId, CancellationToken cancellationToken)
        {
            var spec = new CartByUserIdSpecification(userId);
            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(spec, cancellationToken);

            if (cart == null)
                return;

            var itemToRemove = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.CartItems.Remove(itemToRemove);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}