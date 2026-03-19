using Caching.Cache;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Common.Interfaces.Grpc;
using Shared.Protos.Resource;

namespace Product.Infrastructure.Services.Cache
{
    public class CurriculumCacheService : ICurriculumCacheService
    {
        private readonly ICacheRedis _cache;
        private readonly IGrpcCurriculumClient _grpcCurriculumClient;
        private readonly ILogger<CurriculumCacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromDays(1);

        public CurriculumCacheService(
            ICacheRedis cache,
            IGrpcCurriculumClient grpcCurriculumClient,
            ILogger<CurriculumCacheService> logger
        )
        {
            _grpcCurriculumClient = grpcCurriculumClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CurriculumDetails> GetCurriculumByIdAsync(int id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_Curriculum_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving curriculum with id {CurriculumId} from cache", id);
                var cached = await _cache.GetAsync<CurriculumDetails>(key);
                if (cached != null)
                {
                    _logger.LogInformation("Curriculum {CurriculumId} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for curriculum {CurriculumId}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("Curriculum {CurriculumId} not found in cache ==> calling gRPC", id);

            var curriculum = await _grpcCurriculumClient.GetCurriculumByIdAsync(id);
            if (curriculum == null)
            {
                _logger.LogWarning("Curriculum {CurriculumId} not found via gRPC", id);
                throw new InvalidOperationException($"Curriculum {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, curriculum, _defaultTtl);
                _logger.LogInformation("Curriculum {CurriculumId} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache curriculum {CurriculumId}. Continuing without caching.", id);
            }

            return curriculum;
        }
    }
}
