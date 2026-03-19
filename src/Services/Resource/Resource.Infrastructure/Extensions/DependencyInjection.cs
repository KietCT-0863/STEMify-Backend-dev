using Caching.Cache;
using Contracts.Abstractions.Services;
using Emulator.API.Protos;
using Infrastructure.Abstractions.Services.File;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Common.Interfaces.Grpc;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Infrastructure.Data;
using Resource.Infrastructure.Persistence;
using Resource.Infrastructure.Persistence.Repositories;
using Resource.Infrastructure.Services;
using Resource.Infrastructure.Services.Cache;
using Resource.Infrastructure.Services.Grpc;
using Shared.Protos.User;
using Sieve.Services;

namespace Resource.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            var grpcIdentityUrl = GetGrpcUrl(config, "GrpcIdentityUrl", "identity-api");
            var grpcEmulatorUrl = GetGrpcUrl(config, "GrpcEmulatorUrl", "emulator-api");
            // services.AddDbContext<ResourceDbContext>(opt =>
            // {
            //     opt.UseNpgsql(config.GetConnectionString("stemifyresource"));
            // });
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<IResourceUnitOfWork, ResourceUnitOfWork>();

            services.AddScoped<ICsvParserService, CsvParserService>();
            services.AddScoped<IFileReader, FileReader>();
            services.AddScoped<ResourceDbContextSeed>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<IAgeRangeRepository, AgeRangeRepository>();
            services.AddScoped<ISkillRepository, SkillRepository>();
            services.AddScoped<IStandardRepository, StandardRepository>();
            services.AddScoped<IAnswerRepository, AnswerRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<ISectionRepository, SectionRepository>();
            services.AddScoped<IContentRepository, ContentRepository>();
            services.AddScoped<IQuizRepository, QuizRepository>();
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            services.AddScoped<ICourseLearningOutcomeRepository, CourseLearningOutcomeRepository>();
            services.AddScoped<IProgramLearningOutcomeRepository, ProgramLearningOutcomeRepository>();
            services.AddScoped<ICurriculumCourseRepository, CurriculumCourseRepository>();
            services.AddScoped<ILessonAssetRepository, LessonAssetRepository>();
            services.AddScoped<ILessonAssetTagRepository, LessonAssetTagRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IAssignmentQuestionRepository, AssignmentQuestionRepository>();
            services.AddScoped<IAssignmentRepository, AssignmentRepository>();
            services.AddScoped<IRubricCriterionRepository, RubricCriterionRepository>();
            services.AddScoped<ICurriculumEmulationRepository, CurriculumEmulationRepository>();

            // Register AI services with HttpClient
            services.AddHttpClient<IAgentService, AgentService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddHttpClient<IGeminiCacheService, GeminiCacheService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Add MemoryCache for caching Gemini cached content names
            services.AddMemoryCache();

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
            services.AddConfiguredGrpcClient<EmulatorService.EmulatorServiceClient>(grpcEmulatorUrl);
            services.AddScoped<IGrpcEmulationClient, GrpcEmulationClient>();

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

            //  Fallback 1: Try to construct from Azure Container Apps domain
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
                "GrpcEmulatorUrl" => "https://localhost:7226",
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
