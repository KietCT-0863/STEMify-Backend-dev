using Caching.Cache;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.EnrollmentModels;
using MassTransit.Initializers;
using Microsoft.Extensions.Logging;

namespace Classroom.Infrastructure.Services.Cache
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
                _logger.LogInformation("Retrieving user with id {UserId} from cache", id);
                var cached = await _cache.GetAsync<UserModel>(key);
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

        public async Task<UserModel> GetOrganizationUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var user = await _grpcUserClient.GetOrganizationUserByIdAsync(id).Select(user => new UserModel
            {
                UserId = user.UserId,
                Name = $"{user.FirstName} {user.LastName}",
                Email = user.Email
            });
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found via gRPC", id);
                throw new InvalidOperationException($"User {id} not found from gRPC");
            }

            try
            {
                //await _cache.SetAsync(key, user, _defaultTtl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache user {UserId}. Continuing without caching.", id);
            }

            return user;
        }
    }
}
