using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Emulator.Service.Interfaces;
using Emulator.Service.Services;
using Emulator.Repository.Interfaces;
using Emulator.Repository.Implementations;
using Emulator.Repository.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Caching.Cache;
using Contracts.Abstractions.Services;
using Infrastructure.Abstractions.Services.Cloudinary;



namespace Emulator.Service.Extensions;

/// <summary>
/// Dependency injection extensions for Service layer
/// </summary>
public static class ServiceExtensions
{
    public static IServiceCollection AddServiceServices(
           this IServiceCollection services,
           IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(options =>
        {
            configuration.GetSection("MongoDb").Bind(options);
            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                options.DatabaseName = "stemify-emulator";
            }
        });
       
        services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<IOptions<MongoDbSettings>>();
            return client.GetDatabase(options.Value.DatabaseName);
        });

        var redisHost = configuration["RedisConfig:Host"];
        var redisPort = configuration["RedisConfig:Port"];
        var redisPassword = configuration["RedisConfig:Password"];
        var redisSsl = configuration["RedisConfig:Ssl"];

        if (!string.IsNullOrEmpty(redisHost) && !string.IsNullOrEmpty(redisPort))
        {
            var connectionString = string.IsNullOrEmpty(redisPassword)
                ? $"{redisHost}:{redisPort},ssl={redisSsl},abortConnect=false,connectTimeout=500,syncTimeout=500,connectRetry=2,keepAlive=30,asyncTimeout=500"
                : $"{redisHost}:{redisPort},password={redisPassword},ssl={redisSsl},abortConnect=false,connectTimeout=500,syncTimeout=500,connectRetry=2,keepAlive=30,asyncTimeout=500";

            try
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                });
                Console.WriteLine("Emulator Service: Redis cache configured (localhost:6379)");
                Console.WriteLine(" If Redis is not running, cache operations will fallback gracefully (500ms timeout)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Emulator Service: Failed to configure Redis: {ex.Message}. Falling back to in-memory cache.");
                services.AddDistributedMemoryCache();
            }
        }
        else
        {
            services.AddDistributedMemoryCache();
            Console.WriteLine("Emulator Service: Redis not configured. Using in-memory cache as fallback.");
        }

        services.AddScoped<ICacheRedis, CacheRedis>();

        // Repository registrations
        services.AddScoped<IEmulationRepository, EmulationRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();

        //  Operation Repository 
        services.AddScoped<IOperationRepository, OperationRepository>();

        // Service registrations
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IEmulationService, EmulationService>();
        services.AddScoped<ITemplateService, TemplateService>();

        // Operation Service 
        services.AddScoped<IOperationService, OperationService>();

        // Cloudinary service 
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        return services;

    }
}