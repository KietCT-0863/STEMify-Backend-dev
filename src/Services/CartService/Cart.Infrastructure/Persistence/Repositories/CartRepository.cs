using Cart.Application.Common.Interfaces.Repositories;
using Cart.Application.Models;
using Cart.Infrastructure.Helper;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sieve.Services;

namespace Cart.Infrastructure.Persistence.Repositories
{
    public class CartRepository
        : EfRepositoryBase<CartDbContext, Domain.Entities.Cart, int>,
        ICartRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(
            CartDbContext context,
            ISieveProcessor sieveProcessor,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartRepository> logger)
            : base(context, sieveProcessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        //public async Task<List<CartItemDTO>> SaveCartToCookieAsync(int productId, string? userId, int quantity = 1)
        //{
        //    try
        //    {
        //        var cartItems = await GetCookieCartAsDictionaryAsync(userId);

        //        if (cartItems.TryGetValue(productId, out var item))
        //        {
        //            // Update quantity if item exists
        //            item.Quantity = quantity;
        //        }
        //        else
        //        {
        //            // Add new item
        //            item = new CartItemDTO
        //            {
        //                ItemId = productId,
        //                Quantity = quantity
        //            };
        //            cartItems[productId] = item;
        //        }

        //        var updatedCartList = cartItems.Values.ToList();
        //        await SaveCartItemsToCookieAsync(updatedCartList, userId);

        //        _logger.LogInformation("Saved course {CourseId} with quantity {Quantity} to cookie cart for user {UserId}",
        //            productId, quantity, userId);

        //        return updatedCartList;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving course {CourseId} to cookie cart for user {UserId}", productId, userId);
        //        throw new Exception("An error occurred while saving the cart to cookie: " + ex.Message);
        //    }
        //}

        public async Task<List<CartItemDTO>> AddToCookieCartAsync(int productId, string? userId, int quantityToAdd = 1)
        {
            try
            {
                var cartItems = await GetCookieCartAsDictionaryAsync(userId);

                if (cartItems.TryGetValue(productId, out var item))
                {
                    // Increment quantity if item exists
                    item.Quantity += quantityToAdd;
                }
                else
                {
                    // Add new item
                    item = new CartItemDTO
                    {
                        ItemId = productId,
                        Quantity = quantityToAdd
                    };
                    cartItems[productId] = item;
                }

                var updatedCartList = cartItems.Values.ToList();
                await SaveCartItemsToCookieAsync(updatedCartList, userId);

                _logger.LogInformation("Added {Quantity} of product {ProductId} to cookie cart for user {UserId}",
                    quantityToAdd, productId, userId);

                return updatedCartList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cookie cart for user {UserId}", productId, userId);
                throw new Exception("An error occurred while adding to cart: " + ex.Message);
            }
        }

        public async Task<List<CartItemDTO>> RemoveItemAndGetUpdatedCartAsync(int productId, string? userId)
        {
            try
            {
                var cartItems = await GetCookieCartAsDictionaryAsync(userId);
                cartItems.Remove(productId);

                var updatedCartList = cartItems.Values.ToList();
                await SaveCartItemsToCookieAsync(updatedCartList, userId);

                _logger.LogInformation("Removed product {ProductId} from cookie cart for user {UserId}", productId, userId);

                return updatedCartList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cookie cart for user {UserId}", productId, userId);
                throw new Exception("An error occurred while removing the item: " + ex.Message);
            }
        }

        public async Task<List<CartItemDTO>> UpdateCookieCartItemAsync(int productId, string? userId, int quantityChange)
        {
            try
            {
                // Get current cart from cookie
                var cartItems = await GetCookieCartAsDictionaryAsync(userId);

                // Update quantity
                if (cartItems.ContainsKey(productId))
                {
                    cartItems[productId].Quantity += quantityChange;

                    if (cartItems[productId].Quantity <= 0)
                    {
                        cartItems.Remove(productId);
                    }
                }
                else if (quantityChange > 0)
                {
                    cartItems[productId] = new CartItemDTO
                    {
                        ItemId = productId,
                        Quantity = quantityChange
                    };
                }

                // Save updated cart to cookie
                var updatedCartList = cartItems.Values.ToList();
                await SaveCartItemsToCookieAsync(updatedCartList, userId);

                _logger.LogInformation("Updated cart item {ProductId} with quantity change {QuantityChange} for user {UserId}",
                    productId, quantityChange, userId);

                return updatedCartList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cookie cart item {ProductId} for user {UserId}", productId, userId);
                throw new Exception("An error occurred while updating the cart item: " + ex.Message);
            }
        }

        private async Task<Dictionary<int, CartItemDTO>> GetCookieCartAsDictionaryAsync(string? userId)
        {
            string savedCart;
            if (string.IsNullOrEmpty(userId))
            {
                savedCart = _httpContextAccessor.HttpContext?.Request.Cookies["Cart"] ?? string.Empty;
            }
            else
            {
                savedCart = _httpContextAccessor.HttpContext?.Request.Cookies[$"Cart_{userId}"] ?? string.Empty;
            }

            if (string.IsNullOrEmpty(savedCart))
            {
                return new Dictionary<int, CartItemDTO>();
            }

            return await Task.FromResult(CartUtil.GetCartFromCookie(savedCart));
        }

        private async Task SaveCartItemsToCookieAsync(List<CartItemDTO> cartItems, string? userId)
        {
            var strItemsInCart = CartUtil.ConvertCartToString(cartItems);
            CartUtil.SaveCartToCookie(
                _httpContextAccessor.HttpContext.Request,
                _httpContextAccessor.HttpContext.Response,
                strItemsInCart,
                userId);

            await Task.CompletedTask;
        }

        public async Task RemoveItemFromCookieCartAsync(int courseId, string? userId)
        {
            try
            {
                Dictionary<int, CartItemDTO> cartItems = new Dictionary<int, CartItemDTO>();
                string savedCart;
                if (string.IsNullOrEmpty(userId))
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies["Cart"] ?? string.Empty;
                }
                else
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies[$"Cart_{userId}"] ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(savedCart))
                {
                    cartItems = CartUtil.GetCartFromCookie(savedCart);
                    cartItems.Remove(courseId);
                }

                var strItemsInCart = CartUtil.ConvertCartToString(cartItems.Values.ToList());
                CartUtil.SaveCartToCookie(_httpContextAccessor.HttpContext.Request, _httpContextAccessor.HttpContext.Response, strItemsInCart, userId);

                await Task.CompletedTask;
                _logger.LogInformation("Removed course {CourseId} from cookie cart for user {UserId}", courseId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing course {CourseId} from cookie cart for user {UserId}", courseId, userId);
                throw new Exception("An error occurred while removing the item from the cart: " + ex.Message);
            }
        }

        public async Task DeleteCartInCookieAsync(string? userId)
        {
            try
            {
                CartUtil.DeleteCartToCookie(_httpContextAccessor.HttpContext.Request, _httpContextAccessor.HttpContext.Response, userId);
                await Task.CompletedTask;
                _logger.LogInformation("Deleted cookie cart for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cookie cart for user {UserId}", userId);
                throw new Exception("An error occurred while deleting the cart: " + ex.Message);
            }
        }

        public async Task<int> NumberOfItemsInCookieCartAsync(string? userId)
        {
            try
            {
                int count = 0;
                string savedCart;
                if (string.IsNullOrEmpty(userId))
                {
                    savedCart = _httpContextAccessor.HttpContext.Request.Cookies[$"Cart"];
                }
                else
                {
                    savedCart = _httpContextAccessor.HttpContext.Request.Cookies[$"Cart_{userId}"];
                }
                if (!string.IsNullOrEmpty(savedCart))
                {
                    var cartItems = CartUtil.GetCartFromCookie(savedCart);
                    count = cartItems.Count;
                }
                _logger.LogInformation("Number of items in cookie cart for user {UserId}: {Count}", userId, count);
                return await Task.FromResult(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting number of items in cookie cart for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<CartItemDTO>> GetCookieCartAsync(string? userId)
        {
            try
            {
                string savedCart;
                if (string.IsNullOrEmpty(userId))
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies["Cart"] ?? string.Empty;
                }
                else
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies[$"Cart_{userId}"] ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(savedCart))
                {
                    var cart = CartUtil.GetCartFromCookie(savedCart);
                    var cartItems = cart.Values.ToList();
                    _logger.LogInformation("Retrieved cookie cart for user {UserId} with {Count} items", userId, cartItems.Count);
                    return await Task.FromResult(cartItems);
                }
                _logger.LogInformation("No cookie cart found for user {UserId}", userId);
                return new List<CartItemDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cookie cart for user {UserId}", userId);
                return new List<CartItemDTO>();
            }
        }

        public async Task SaveCartToCookieAsync(int productId, string? userId, int quantity = 1)
        {
            try
            {
                Dictionary<int, CartItemDTO> cartItems = new Dictionary<int, CartItemDTO>();

                string savedCart;
                if (string.IsNullOrEmpty(userId))
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies["Cart"] ?? string.Empty;
                }
                else
                {
                    savedCart = _httpContextAccessor.HttpContext?.Request.Cookies[$"Cart_{userId}"] ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(savedCart))
                {
                    cartItems = CartUtil.GetCartFromCookie(savedCart);
                }

                if (cartItems.TryGetValue(productId, out var item))
                {
                    // Update quantity if item exists
                    item.Quantity = quantity;
                }
                else
                {
                    // Add new item
                    item = new CartItemDTO
                    {
                        ItemId = productId,
                        Quantity = quantity
                    };
                    cartItems[productId] = item;
                }

                var strItemsInCart = CartUtil.ConvertCartToString(cartItems.Values.ToList());
                CartUtil.SaveCartToCookie(_httpContextAccessor.HttpContext.Request, _httpContextAccessor.HttpContext.Response, strItemsInCart, userId);

                await Task.CompletedTask;
                _logger.LogInformation("Saved course {CourseId} with quantity {Quantity} to cookie cart for user {UserId}", productId, quantity, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving course {CourseId} to cookie cart for user {UserId}", productId, userId);
                throw new Exception("An error occurred while saving the cart to cookie: " + ex.Message);
            }
        }
    }
}