using Caching.Cache;
using Cart.Application.Common.Interfaces.Cache;
using Cart.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Cart.Infrastructure.Services.Cache
{
    public class UserCacheService : IUserCacheService
    {
        private readonly ICacheRedis _cache;
        private readonly IGrpcUserClient _grpcUserClient;
        private readonly ILogger<UserCacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromDays(1);

        public UserCacheService(
            ICacheRedis cache,
            IGrpcUserClient grpcUserClient,
            ILogger<UserCacheService> logger
        )
        {
            _grpcUserClient = grpcUserClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<GrpcUserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_User_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving user with id {UserId} from cache", id);
                var cached = await _cache.GetAsync<GrpcUserResponse>(key);
                if (cached != null)
                {
                    _logger.LogInformation("User {UserId} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for user {UserId}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("User {UserId} not found in cache ==> calling gRPC", id);

            var user = await _grpcUserClient.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found via gRPC", id);
                throw new InvalidOperationException($"User {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, user, _defaultTtl);
                _logger.LogInformation("User {UserId} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache user {UserId}. Continuing without caching.", id);
            }

            return user;
        }
    }
}
