using Caching.Cache;
using Microsoft.Extensions.Logging;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Common.Interfaces.Grpc;
using Shared.Protos.Resource;

namespace Product.Infrastructure.Services.Cache
{
    public class CourseCacheService : ICourseCacheService
    {
        private readonly ICacheRedis _cache;
        private readonly IGrpcCourseClient _grpcCourseClient;
        private readonly ILogger<CourseCacheService> _logger;
        private readonly TimeSpan _defaultTtl = TimeSpan.FromDays(1);

        public CourseCacheService(
            ICacheRedis cache,
            IGrpcCourseClient grpcCourseClient,
            ILogger<CourseCacheService> logger
        )
        {
            _grpcCourseClient = grpcCourseClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CourseDetail> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var key = $"{CacheKeys.Key_Course_By_Id}{id}";

            try
            {
                _logger.LogInformation("Retrieving course with id {CourseId} from cache", id);
                var cached = await _cache.GetAsync<CourseDetail>(key);
                if (cached != null)
                {
                    _logger.LogInformation("Course {CourseId} found in cache", id);
                    return cached;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache retrieval failed for course {CourseId}. Falling back to gRPC.", id);
            }

            _logger.LogInformation("Course {CourseId} not found in cache ==> calling gRPC", id);

            var course = await _grpcCourseClient.GetCourseByIdAsync(id);
            if (course == null)
            {
                _logger.LogWarning("Course {CourseId} not found via gRPC", id);
                throw new InvalidOperationException($"Course {id} not found from gRPC");
            }

            try
            {
                await _cache.SetAsync(key, course, _defaultTtl);
                _logger.LogInformation("Course {CourseId} cached successfully", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache course {CourseId}. Continuing without caching.", id);
            }

            return course;
        }
    }
}
