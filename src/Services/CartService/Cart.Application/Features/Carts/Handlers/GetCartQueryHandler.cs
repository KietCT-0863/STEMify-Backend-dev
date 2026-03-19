using Cart.Application.Common.Interfaces;
using Cart.Application.Common.Interfaces.Cache;
using Cart.Application.Features.Carts.Queries;
using Cart.Application.Specifications;
using MediatR;
using Shared.Protos.Cart;
using Shared.Protos.Product;

namespace Cart.Application.Features.Carts.Handlers
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartResponse>
    {
        private readonly ICartUnitOfWork _unitOfWork;
        private readonly IProductCacheService _productCacheService;
        private readonly IUserCacheService _userCacheService;

        public GetCartQueryHandler(
            ICartUnitOfWork unitOfWork,
            IProductCacheService productCacheService,
            IUserCacheService userCacheService)
        {
            _unitOfWork = unitOfWork;
            _productCacheService = productCacheService;
            _userCacheService = userCacheService;
        }

        public async Task<CartResponse> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var userId = request.UserId;
            var isAuthenticated = !string.IsNullOrEmpty(userId);

            if (isAuthenticated)
            {
                // Get cart from database for authenticated users
                return await GetDatabaseCartAsync(userId, cancellationToken);
            }
            else
            {
                // Get cart from cookie for guest users
                return await GetCookieCartAsync(userId, cancellationToken);
            }
        }

        private async Task<CartResponse> GetDatabaseCartAsync(string userId, CancellationToken cancellationToken)
        {
            // Get cart from database
            var spec = new CartByUserIdSpecification(userId);
            var cart = await _unitOfWork.Carts.FirstOrDefaultAsync(spec, cancellationToken);

            if (cart == null || !cart.CartItems.Any())
            {
                return new CartResponse
                {
                    UserId = userId,
                    TotalPrice = 0
                };
            }

            // Get product details from cache
            var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
            var products = new List<ProductResponse>();
            foreach (var productId in productIds)
            {
                var product = await _productCacheService.GetByIdAsync(productId, cancellationToken);
                if (product != null)
                    products.Add(product);
            }

            var cartItemResponses = new List<CartItemResponse>();
            double totalPrice = 0;

            foreach (var cartItem in cart.CartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product != null)
                {
                    var subtotal = product.Price * cartItem.Quantity;
                    totalPrice += subtotal;

                    cartItemResponses.Add(new CartItemResponse
                    {
                        ProductId = cartItem.ProductId,
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
                UserId = userId,
                Items = { cartItemResponses },
                TotalPrice = totalPrice
            };
        }

        private async Task<CartResponse> GetCookieCartAsync(string userId, CancellationToken cancellationToken)
        {
            // Get cart items from cookie
            var cookieCartItems = await _unitOfWork.Carts.GetCookieCartAsync(userId);

            if (!cookieCartItems.Any())
            {
                return new CartResponse
                {
                    UserId = string.IsNullOrEmpty(userId) ? null : userId,
                    TotalPrice = 0
                };
            }

            // Get product details from cache
            var productIds = cookieCartItems.Select(ci => ci.ItemId).ToList();
            var products = new List<ProductResponse>();
            foreach (var productId in productIds)
            {
                var product = await _productCacheService.GetByIdAsync(productId, cancellationToken);
                if (product != null)
                    products.Add(product);
            }

            var cartItemResponses = new List<CartItemResponse>();
            double totalPrice = 0;

            foreach (var cookieItem in cookieCartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == cookieItem.ItemId);
                if (product != null)
                {
                    var subtotal = product.Price * cookieItem.Quantity;
                    totalPrice += subtotal;

                    cartItemResponses.Add(new CartItemResponse
                    {
                        ProductId = cookieItem.ItemId,
                        Name = product.Name,
                        Description = product.Description ?? string.Empty,
                        ImageUrl = product.ImageUrl ?? string.Empty,
                        Quantity = cookieItem.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = subtotal
                    });
                }
            }

            return new CartResponse
            {
                UserId = string.IsNullOrEmpty(userId) ? null : userId,
                Items = { cartItemResponses },
                TotalPrice = totalPrice
            };
        }
    }
}