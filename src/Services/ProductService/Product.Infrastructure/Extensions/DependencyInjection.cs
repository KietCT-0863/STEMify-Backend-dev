using Caching.Cache;
using Contracts.Abstractions.Services;
using Infrastructure.Abstractions.Services.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Common.Interfaces.Grpc;
using Product.Application.Common.Interfaces.Repositories;
using Product.Infrastructure.Persistence;
using Product.Infrastructure.Persistence.Repositories;
using Product.Infrastructure.Services.Cache;
using Product.Infrastructure.Services.Grpc;
using Shared.Extensions;
using Shared.Protos.Resource;
using Shared.Protos.User;
using Sieve.Services;

namespace Product.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            var grpcIdentityUrl = GetGrpcUrl(config, "GrpcIdentityUrl", "identity-api");
            var grpcResourceUrl = GetGrpcUrl(config, "GrpcResourceUrl", "resource-api");

            // services.AddDbContext<ResourceDbContext>(opt =>
            // {
            //     opt.UseNpgsql(config.GetConnectionString("stemifyresource"));
            // });
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<ProductDbContextSeed>();

            services.AddScoped<IProductUnitOfWork, ProductUnitOfWork>();

            services.AddScoped<IFileReader, FileReader>();
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<IPlanBillingCycleRepository, PlanBillingCycleRepository>();
            services.AddScoped<IPlanCurriculumRepository, PlanCurriculumRepository>();
            services.AddScoped<IKitProductRepository, KitProductRepository>();
            services.AddScoped<IKitImageRepository, KitImageRepository>();
            services.AddScoped<IComponentRepository, ComponentRepository>();
            services.AddScoped<IKitComponentRepository, KitComponentRepository>();

            services.AddScoped<ICacheRedis, CacheRedis>();
            services.AddScoped<ICourseCacheService, CourseCacheService>();
            services.AddScoped<IUserCacheService, UserCacheService>();
            services.AddScoped<ICurriculumCacheService, CurriculumCacheService>();

            // Add Grpc client factory
            services.AddConfiguredGrpcClient<CourseService.CourseServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<GrpcUser.GrpcUserClient>(grpcIdentityUrl);
            services.AddConfiguredGrpcClient<LessonService.LessonServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<CurriculumService.CurriculumServiceClient>(grpcResourceUrl);

            services.AddScoped<IGrpcCourseClient, GrpcCourseClient>();
            services.AddScoped<IGrpcUserClient, GrpcUserClient>();
            services.AddScoped<IGrpcCurriculumClient, GrpcCurriculumClient>();


            // Add cache service
            var redisHost = config["RedisConfig:Host"];
            var redisPort = config["RedisConfig:Port"];
            var redisPassword = config["RedisConfig:Password"];
            var redisSsl = config["RedisConfig:Ssl"];

            if (!string.IsNullOrEmpty(redisHost) && !string.IsNullOrEmpty(redisPort))
            {
                var connectionString = string.IsNullOrEmpty(redisPassword)
                    ? $"{redisHost}:{redisPort},ssl={redisSsl},abortConnect=false,connectTimeout=2000,syncTimeout=2000,connectRetry=3,keepAlive=60,asyncTimeout=2000"
                    : $"{redisHost}:{redisPort},password={redisPassword},ssl={redisSsl},abortConnect=false,connectTimeout=2000,syncTimeout=2000,connectRetry=3,keepAlive=60,asyncTimeout=2000";

                try
                {
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = connectionString;
                    });
                    Console.WriteLine("Redis cache configured successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to configure Redis: {ex.Message}. Falling back to in-memory cache.");
                    services.AddDistributedMemoryCache();
                }
            }
            else
            {
                services.AddDistributedMemoryCache();
                Console.WriteLine("Redis not configured. Using in-memory cache as fallback.");
            }

            services.AddScoped<ICacheRedis, CacheRedis>();
            services.AddConfiguredGrpcClient<GrpcUser.GrpcUserClient>(grpcIdentityUrl);

            return services;
        }

        private static string GetGrpcUrl(
            IConfiguration config,
            string configKey,
            string serviceName
        )
        {
            // Try to get from configuration first
            var configUrl = config[configKey];

            if (!string.IsNullOrEmpty(configUrl))
            {
                return configUrl;
            }

            // Fallback 1: Try to construct from Azure Container Apps domain
            var containerAppsDomain = Environment.GetEnvironmentVariable(
                "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"
            );
            if (!string.IsNullOrEmpty(containerAppsDomain))
            {
                var azureUrl = $"http://{serviceName}.{containerAppsDomain}";
                Console.WriteLine(
                    $"[{configKey}] not found in config. Using Azure fallback: {azureUrl}"
                );
                return azureUrl;
            }

            // Fallback 2: Development/localhost URLs
            var fallbackUrl = configKey switch
            {
                "GrpcResourceUrl" => "https://localhost:7003",
                "GrpcIdentityUrl" => "https://localhost:7002",
                _ => "http://localhost:8080",
            };

            Console.WriteLine(
                $"[{configKey}] not found. Using development fallback: {fallbackUrl}"
            );
            Console.WriteLine(
                $" Available config keys: {string.Join(", ", config.AsEnumerable().Select(c => c.Key))}"
            );

            return fallbackUrl;
        }
    }
}
