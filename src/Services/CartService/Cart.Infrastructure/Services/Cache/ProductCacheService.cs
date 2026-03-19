using Caching.Cache;
using Cart.Application.Common.Interfaces.Cache;
using Cart.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.Product;

namespace Cart.Infrastructure.Services.Cache
{
    public class ProductCacheService : IProductCacheService
    {
        private readonly ICacheRedis _cache;
        private readonly IGrpcProductClient _grpcProductClient;
        private readonly ILogger<ProductCacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromDays(1);

        public ProductCacheService(
            ICacheRedis cache,
            IGrpcProductClient grpcProductClient,
            ILogger<ProductCacheService> logger
        )
        {
            _grpcProductClient = grpcProductClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_Product_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving product with id {ProductId} from cache", id);
                var cached = await _cache.GetAsync<ProductResponse>(key);
                if (cached != null)
                {
                    _logger.LogInformation("Product {ProductId} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for product {ProductId}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("Product {ProductId} not found in cache ==> calling gRPC", id);

            var product = await _grpcProductClient.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found via gRPC", id);
                throw new InvalidOperationException($"Product {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, product, _defaultTtl);
                _logger.LogInformation("Product {ProductId} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache product {ProductId}. Continuing without caching.", id);
            }

            return product;
        }
    }
}
