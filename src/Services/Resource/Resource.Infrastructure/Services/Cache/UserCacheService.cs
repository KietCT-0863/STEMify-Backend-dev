using Caching.Cache;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Common.Interfaces.Grpc;
using Resource.Application.Models.User;

namespace Resource.Infrastructure.Services.Cache
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

        public async Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_User_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving user with id {id} from cache", id);

                // Check cache
                var cached = await _cache.GetAsync<UserModel>(key);
                if (cached != null)
                {
                    _logger.LogInformation("User {id} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for user {id}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("User {id} not found in cache ==> calling gRPC", id);

            // Call gRPC
            var user = await _grpcUserClient.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {id} not found via gRPC", id);
                throw new InvalidOperationException($"User {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, user, _defaultTtl);
                _logger.LogInformation("User {id} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache user {id}. Continuing without caching.", id);
            }

            return user;
        }
    }
}