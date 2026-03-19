using Contracts.Abstractions.Services;
using Contracts.Configurations;
using Infrastructure.Abstractions.Services.File;
using Infrastructure.Extensions;
using Infrastructure.HealthChecks;
using Infrastructure.Idempotency;
using Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notification.Application.Common.Configurations;
using Notification.Application.Common.Interfaces;
using Notification.Application.Common.Interfaces.Repositories;
using Notification.Application.Common.Interfaces.Services;
using Notification.Infrastructure.Data;
using Notification.Infrastructure.HealthChecks;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Services;
using Sieve.Services;

namespace Notification.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            // services.AddDbContext<NotificationDbContext>(opt =>
            // {
            //     opt.UseNpgsql(config.GetConnectionString("stemifynotification"));
            // });
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<INotificationUnitOfWork, NotificationUnitOfWork>();

            services.AddScoped<IFileReader, FileReader>();

            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IDeviceRepository, DeviceRepository>();

            // Add Application Services
            services.AddScoped<IFCMNotificationService, FCMNotificationService>();
            services.AddScoped<IDeviceService, DeviceService>();
            services.AddScoped<
                Application.Common.Interfaces.Services.IEmailService,
                EmailService
            >();

            
            services.Configure<global::Infrastructure.Configurations.SMTPEmailSetting>(config.GetSection("EmailSettings"));

            services.AddSingleton<IEmailSettings>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<global::Infrastructure.Configurations.SMTPEmailSetting>>().Value;
                return settings; 
            });

            services.AddSingleton<IOptions<IEmailSettings>>(provider =>
            {
                var concreteSettings = provider.GetRequiredService<IOptions<global::Infrastructure.Configurations.SMTPEmailSetting>>();
                return Options.Create<IEmailSettings>(concreteSettings.Value);
            });

            services.AddSingleton<Contracts.Abstractions.Services.IEmailTemplateService, global::Infrastructure.Services.EmailTemplateService>();
            services.AddScoped<Contracts.Abstractions.Services.IEmailService, global::Infrastructure.Services.EmailService>();

            // Add Infrastructure building blocks services
            services.AddPollyResilience(config);
            services.AddIdempotency();

            // Add Database Health Check
            services.AddDatabaseHealthCheck<NotificationDbContext>();

            // Add Notification-specific Health Check
            services.AddScoped<IHealthCheckService, NotificationHealthCheck>();

            return services;
        }
    }
}
