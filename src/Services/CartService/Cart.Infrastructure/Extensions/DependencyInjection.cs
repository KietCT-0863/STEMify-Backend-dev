using Caching.Cache;
using Cart.Application.Common.Interfaces;
using Cart.Application.Common.Interfaces.Cache;
using Cart.Application.Common.Interfaces.Grpc;
using Cart.Application.Common.Interfaces.Repositories;
using Cart.Infrastructure.Persistence;
using Cart.Infrastructure.Persistence.Repositories;
using Cart.Infrastructure.Services.Cache;
using Cart.Infrastructure.Services.Grpc;
using Contracts.Abstractions.Services;
using Infrastructure.Abstractions.Services.Data;
using Infrastructure.Abstractions.Services.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Shared.Protos.Product;
using Shared.Protos.User;
using Sieve.Services;

namespace Cart.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            var grpcIdentityUrl = GetGrpcUrl(config, "GrpcIdentityUrl", "identity-api");
            var grpcProductUrl = GetGrpcUrl(config, "GrpcProductUrl", "product-api");
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<ICartUnitOfWork, CartUnitOfWork>();

            services.AddScoped<IFileReader, FileReader>();
            services.AddDistributedMemoryCache();
            services.AddHttpContextAccessor();
            services.AddScoped<ISerializeService, SerializeService>();
            services.AddScoped<ICartRepository, CartRepository>();

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
            services.AddScoped<IUserCacheService, UserCacheService>();
            services.AddScoped<IGrpcUserClient, GrpcUserClient>();

            services.AddConfiguredGrpcClient<ProductService.ProductServiceClient>(grpcProductUrl);
            services.AddScoped<IProductCacheService, ProductCacheService>();
            services.AddScoped<IGrpcProductClient, GrpcProductClient>();

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

            // ✅ Fallback 1: Try to construct from Azure Container Apps domain
            var containerAppsDomain = Environment.GetEnvironmentVariable(
                "AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"
            );
            if (!string.IsNullOrEmpty(containerAppsDomain))
            {
                var azureUrl = $"http://{serviceName}.{containerAppsDomain}";
                Console.WriteLine(
                    $"⚠️ [{configKey}] not found in config. Using Azure fallback: {azureUrl}"
                );
                return azureUrl;
            }

            // ✅ Fallback 2: Development/localhost URLs
            var fallbackUrl = configKey switch
            {
                "GrpcResourceUrl" => "https://localhost:7003",
                "GrpcIdentityUrl" => "https://localhost:7002",
                "GrpcProductUrl" => "https://localhost:7005",
                _ => "http://localhost:8080",
            };

            Console.WriteLine(
                $"❌ [{configKey}] not found. Using development fallback: {fallbackUrl}"
            );
            Console.WriteLine(
                $"🔍 Available config keys: {string.Join(", ", config.AsEnumerable().Select(c => c.Key))}"
            );

            return fallbackUrl;
        }
    }
}
