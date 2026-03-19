using Shared.Protos.Product;

namespace Cart.Application.Common.Interfaces.Cache
{
    public interface IProductCacheService
    {
        Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
