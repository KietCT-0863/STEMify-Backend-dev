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
    public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartResponse>
    {
        private readonly ICartUnitOfWork _unitOfWork;
        private readonly IProductCacheService _productCacheService;
        private readonly IMediator _mediator;

        public UpdateCartItemCommandHandler(
            ICartUnitOfWork unitOfWork,
            IProductCacheService productCacheService,
            IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _productCacheService = productCacheService;
            _mediator = mediator;
        }

        public async Task<CartResponse> Handle(
            UpdateCartItemCommand request,
            CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var isAuthenticated = !string.IsNullOrEmpty(userId);

            // Validate product exists
            var product = await _productCacheService.GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
            {
                throw new ArgumentException($"Product with ID {request.ProductId} not found");
            }

            if (isAuthenticated)
            {
                // Update database cart for authenticated users
                await UpdateDatabaseCartAsync(userId, request.ProductId, request.Quantity, cancellationToken);

                var getCartQuery = new GetCartQuery { UserId = request.UserId };
                return await _mediator.Send(getCartQuery, cancellationToken);
            }
            else
            {
                // Update cookie cart for guest users
                var updatedCartItems = await _unitOfWork.Carts.UpdateCookieCartItemAsync(
                    request.ProductId,
                    userId,
                    request.Quantity);

                // Build and return response from updated data
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

        private async Task UpdateDatabaseCartAsync(string userId, int productId, int quantityChange, CancellationToken cancellationToken)
        {
            // Get or create cart
            var spec = new CartByUserIdSpecification(userId);
            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(spec, cancellationToken);

            if (cart == null)
            {
                // Create new cart
                cart = new Domain.Entities.Cart
                {
                    UserId = userId,
                    CartItems = new List<Domain.Entities.CartItem>()
                };
                await _unitOfWork.Carts.AddAsync(cart, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Check if item already exists
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                // Increment or decrement the quantity
                existingItem.Quantity += quantityChange;

                // Remove item if quantity becomes 0 or negative
                if (existingItem.Quantity <= 0)
                {
                    cart.CartItems.Remove(existingItem);
                }
            }
            else if (quantityChange > 0)
            {
                // Add new item only if quantityChange is positive
                var newItem = new Domain.Entities.CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantityChange
                };
                cart.CartItems.Add(newItem);
            }
            // If quantityChange is negative and item doesn't exist, do nothing

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}