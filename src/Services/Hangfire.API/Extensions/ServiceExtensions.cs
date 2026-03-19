using Contracts.Abstractions.Services;
using Hangfire.API.Jobs;
using Hangfire.PostgreSql;
using Identity.Application.Extensions;
using Identity.Infrastructure;
using Infrastructure.Services;
using MassTransit;

namespace Hangfire.API.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddServsices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            var hangfireConnectionString = config.GetConnectionString("stemifyhangfire");
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c =>
                    c.UseNpgsqlConnection(hangfireConnectionString)));

            // Add Hangfire server
            services.AddHangfireServer(options =>
            {
                options.ServerName = $"Hangfire.API-{Environment.MachineName}";
                options.WorkerCount = Environment.ProcessorCount * 2;
                options.Queues = new[] { "default", "critical", "normal" };
            });

            AddMassTransitConfiguration(services, config);

            
            services.AddApplicationServices();
            services.AddInfrastructureServices(config, enableConsumers: false); 

            // Register background jobs
            services.AddScoped<SubscriptionLifecycleJob>();
            services.AddScoped<ScheduledInvitationEmailJob>();
            services.AddSingleton<Contracts.Abstractions.Services.IEmailTemplateService, global::Infrastructure.Services.EmailTemplateService>();
            services.AddScoped<Contracts.Abstractions.Services.IEmailService, global::Infrastructure.Services.EmailService>();

            return services;
        }

        private static void AddMassTransitConfiguration(
        IServiceCollection services,
        IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                // Configure RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var connectionString = configuration.GetConnectionString("messaging")
                        ?? configuration["RabbitMq:Url"]
                        ?? configuration["RabbitMQ:ConnectionString"];

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        // Use connection string (Aspire or full URI format)
                        cfg.Host(new Uri(connectionString));
                    }
                    else
                    {
                        // Fallback to individual configuration values (backward compatibility)
                        var rabbitMqSection = configuration.GetSection("RabbitMQ");
                        var host = rabbitMqSection["Host"] ?? "localhost";
                        var port = rabbitMqSection.GetValue<ushort>("Port", 5672);
                        var username = rabbitMqSection["Username"] ?? "guest";
                        var password = rabbitMqSection["Password"] ?? "guest";
                        var virtualHost = rabbitMqSection["VirtualHost"] ?? "/";

                        cfg.Host(host, port, virtualHost, h =>
                        {
                            h.Username(username);
                            h.Password(password);
                        });
                    }
                    // Global retry
                    cfg.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)));
                });
            });
        }
    }
}
