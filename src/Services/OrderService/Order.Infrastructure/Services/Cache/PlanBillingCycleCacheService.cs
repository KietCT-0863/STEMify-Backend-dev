using Caching.Cache;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Protos.Product;

namespace Order.Infrastructure.Services.Cache
{
    public class PlanBillingCycleCacheService : IPlanBillingCycleCacheService
    {
        private readonly ICacheRedis _cache;
        private readonly IGrpcPlanBillingCycleClient _grpcPlanBillingCycleClient;
        private readonly ILogger<PlanBillingCycleCacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromDays(1);

        public PlanBillingCycleCacheService(
            ICacheRedis cache,
            IGrpcPlanBillingCycleClient grpcPlanBillingCycleClient,
            ILogger<PlanBillingCycleCacheService> logger
        )
        {
            _grpcPlanBillingCycleClient = grpcPlanBillingCycleClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<GrpcPlanBillingCycleModel> GetPlanBillingCycleByIdAsync(int id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_Plan_Billing_Cycle_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving curriculum with id {PlanBillingCycleId} from cache", id);
                var cached = await _cache.GetAsync<GrpcPlanBillingCycleModel>(key);
                if (cached != null)
                {
                    _logger.LogInformation("PlanBillingCycle {PlanBillingCycleId} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for curriculum {PlanBillingCycleId}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("PlanBillingCycle {PlanBillingCycleId} not found in cache ==> calling gRPC", id);

            var curriculum = await _grpcPlanBillingCycleClient.GetPlanBillingCycleByIdAsync(id);
            if (curriculum == null)
            {
                _logger.LogWarning("PlanBillingCycle {PlanBillingCycleId} not found via gRPC", id);
                throw new InvalidOperationException($"PlanBillingCycle {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, curriculum, _defaultTtl);
                _logger.LogInformation("PlanBillingCycle {PlanBillingCycleId} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache curriculum {PlanBillingCycleId}. Continuing without caching.", id);
            }

            return curriculum;
        }
    }
}
