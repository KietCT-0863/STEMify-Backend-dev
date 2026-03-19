using Caching.Cache;
using Contracts.Abstractions.Services;
using Infrastructure.Abstractions.Services.Cloudinary;
using Infrastructure.Abstractions.Services.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Order.Infrastructure.Repositories;
using Order.Infrastructure.Services.Cache;
using Order.Infrastructure.Services.Grpc;
using Shared.Extensions;
using Shared.Protos.Classroom;
using Shared.Protos.Product;
using Shared.Protos.Resource;
using Shared.Protos.User;
using Sieve.Services;

namespace Order.Infrastructure.Extensions
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
            var grpcProductUrl = GetGrpcUrl(config, "GrpcProductUrl", "product-api");
            var grpcClassroomUrl = GetGrpcUrl(config, "GrpcClassroomUrl", "classroom-api");

            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<OrderDbContextSeed>();

            services.AddScoped<IOrderUnitOfWork, OrderUnitOfWork>();

            services.AddScoped<IFileReader, FileReader>();

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
            services.AddConfiguredGrpcClient<CurriculumService.CurriculumServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<GrpcPlanBillingCycleService.GrpcPlanBillingCycleServiceClient>(grpcProductUrl);
            services.AddConfiguredGrpcClient<GrpcClassroom.GrpcClassroomClient>(grpcClassroomUrl);
            services.AddConfiguredGrpcClient<GrpcCertificate.GrpcCertificateClient>(grpcClassroomUrl);
            services.AddConfiguredGrpcClient<GrpcQuizAttempt.GrpcQuizAttemptClient>(grpcClassroomUrl);
            services.AddConfiguredGrpcClient<GrpcAssignmentAttempt.GrpcAssignmentAttemptClient>(grpcClassroomUrl);
            services.AddConfiguredGrpcClient<GrpcCurriculumEnrollment.GrpcCurriculumEnrollmentClient>(grpcClassroomUrl);
            services.AddConfiguredGrpcClient<GrpcCourseEnrollment.GrpcCourseEnrollmentClient>(grpcClassroomUrl);

            services.AddScoped<ICloudinaryService, CloudinaryService>();

            services.AddScoped<IPlanBillingCycleCacheService, PlanBillingCycleCacheService>();
            services.AddScoped<IGrpcPlanBillingCycleClient, GrpcPlanBillingCycleClient>();

            services.AddScoped<IUserCacheService, UserCacheService>();
            services.AddScoped<IGrpcUserClient, GrpcUserClient>();

            services.AddScoped<ICurriculumCacheService, CurriculumCacheService>();
            services.AddScoped<IGrpcCurriculumClient, GrpcCurriculumClient>();

            services.AddScoped<IGrpcClassroomClient, GrpcClassroomClient>();
            services.AddScoped<IGrpcCurriculumEnrollmentClient, GrpcCurriculumEnrollmentClient>();
            services.AddScoped<IGrpcCourseEnrollmentClient, GrpcCourseEnrollmentClient>();
            services.AddScoped<IGrpcCertificateClient, GrpcCertificateClient>();
            services.AddScoped<IGrpcQuizAttemptClient, GrpcQuizAttemptClient>();
            services.AddScoped<IGrpcAssignmentAttemptClient, GrpcAssignmentAttemptClient>();
            // Register repositories
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<ILicenseAssignmentRepository, LicenseAssignmentRepository>();
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IOrganizationSubscriptionOrderRepository, OrganizationSubscriptionOrderRepository>();
            services.AddScoped<IOrganizationTypeRepository, OrganizationTypeRepository>();
            services.AddScoped<ISubscriptionOrderCurriculumRepository, SubscriptionOrderCurriculumRepository>();

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

            //  Fallback 2: Development/localhost URLs
            var fallbackUrl = configKey switch
            {
                "GrpcClassroomUrl" => "https://localhost:7001",
                "GrpcResourceUrl" => "https://localhost:7003",
                "GrpcIdentityUrl" => "https://localhost:7002",
                "GrpcProductUrl" => "https://localhost:7005",
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
