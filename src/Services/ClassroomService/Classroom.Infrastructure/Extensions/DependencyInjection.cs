using Caching.Cache;
using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Infrastructure.Persistence;
using Classroom.Infrastructure.Persistence.Repositories;
using Classroom.Infrastructure.Services.Cache;
using Classroom.Infrastructure.Services.Grpc;
using Contracts.Abstractions.Services;
using Infrastructure.Abstractions.Services.Cloudinary;
using Infrastructure.Abstractions.Services.File;
using Infrastructure.Abstractions.Services.PdfLayer;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Protos.Order;
using Shared.Protos.Resource;
using Shared.Protos.User;
using Sieve.Services;

namespace Classroom.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            var grpcResourceUrl = GetGrpcUrl(config, "GrpcResourceUrl", "resource-api");
            var grpcIdentityUrl = GetGrpcUrl(config, "GrpcIdentityUrl", "identity-api");
            var grpcOrderUrl = GetGrpcUrl(config, "GrpcOrderUrl", "order-api");

            // Registers app services
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<IClassroomUnitOfWork, ClassroomUnitOfWork>();

            services.AddScoped<ICloudinaryService, CloudinaryService>();

            services.AddScoped<IPdfService, PdfService>();

            services.AddScoped<IFileReader, FileReader>();

            services.AddScoped<IAnnoucementRepository, AnnoucementRepository>();
            services.AddScoped<IClassroomRepository, ClassroomRepository>();
            services.AddScoped<ICourseEnrollmentRepository, CourseEnrollmentRepository>();
            services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
            services.AddScoped<ISectionProgressRepository, SectionProgressRepository>();
            services.AddScoped<ICurriculumEnrollmentRepository, CurriculumEnrollmentRepository>();
            services.AddScoped<ICertificateRepository, CertificateRepository>();
            services.AddScoped<IStudentQuizRepository, StudentQuizRepository>();
            services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
            services.AddScoped<IClassroomStudentRepository, ClassroomStudentRepository>();
            services.AddScoped<IRubricScoreRepository, RubricScoreRepository>();
            services.AddScoped<IAssignmentAttemptRepository, AssignmentAttemptRepository>();
            services.AddScoped<IAssignmentQuestionAttemptRepository, AssignmentQuestionAttemptRepository>();
            services.AddScoped<IStudentAssignmentRepository, StudentAssignmentRepository>();

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
            services.AddScoped<ICourseCacheService, CourseCacheService>();
            services.AddScoped<IUserCacheService, UserCacheService>();
            services.AddScoped<ICurriculumCacheService, CurriculumCacheService>();

            // Add Grpc client factory
            services.AddConfiguredGrpcClient<CourseService.CourseServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<GrpcUser.GrpcUserClient>(grpcIdentityUrl);
            services.AddConfiguredGrpcClient<LessonService.LessonServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<CurriculumService.CurriculumServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<ContentService.ContentServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<QuizService.QuizServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<GrpcOrganizationSubscriptionOrderService.GrpcOrganizationSubscriptionOrderServiceClient>(grpcOrderUrl);
            services.AddConfiguredGrpcClient<GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient>(grpcOrderUrl);
            services.AddConfiguredGrpcClient<GrpcAssignmentService.GrpcAssignmentServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<RubricCriterionService.RubricCriterionServiceClient>(grpcResourceUrl);
            services.AddConfiguredGrpcClient<SectionService.SectionServiceClient>(grpcResourceUrl);

            services.AddScoped<IGrpcSectionClient, GrpcSectionClient>();
            services.AddScoped<IGrpcAssignmentClient, GrpcAssignmentClient>();
            services.AddScoped<IGrpcRubricCriterionClient, GrpcRubricCriterionClient>();
            services.AddScoped<IGrpcCourseClient, GrpcCourseClient>();
            services.AddScoped<IGrpcUserClient, GrpcUserClient>();
            services.AddScoped<IGrpcLessonClient, GrpcLessonClient>();
            services.AddScoped<IGrpcCurriculumClient, GrpcCurriculumClient>();
            services.AddScoped<IGrpcContentClient, GrpcContentClient>();
            services.AddScoped<IGrpcQuizClient, GrpcQuizClient>();
            services.AddScoped<IGrpcOrganizationSubscriptionOrderClient, GrpcOrganizationSubscriptionOrderClient>();

            return services;
        }

        /// <summary>
        ///  Get gRPC URL with validation and fallback logic
        /// </summary>
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
                "GrpcResourceUrl" => "https://localhost:7003",
                "GrpcIdentityUrl" => "https://localhost:7002",
                "GrpcOrderUrl" => "https://localhost:7006",
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
