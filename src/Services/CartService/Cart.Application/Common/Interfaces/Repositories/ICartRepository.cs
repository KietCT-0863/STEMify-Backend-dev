using Cart.Application.Models;
using Contracts.Abstractions.Persistence;

namespace Cart.Application.Common.Interfaces.Repositories
{
    public interface ICartRepository : IRepositoryBaseAsync<Domain.Entities.Cart, int>
    {
        Task RemoveItemFromCookieCartAsync(int courseId, string? userId);
        Task DeleteCartInCookieAsync(string? userId);
        Task<int> NumberOfItemsInCookieCartAsync(string? userId);
        Task<List<CartItemDTO>> GetCookieCartAsync(string? userId);
        Task SaveCartToCookieAsync(int productId, string? userId, int quantity = 1);
        Task<List<CartItemDTO>> UpdateCookieCartItemAsync(int productId, string? userId, int quantityChange);
        Task<List<CartItemDTO>> RemoveItemAndGetUpdatedCartAsync(int productId, string? userId);
    }
}
