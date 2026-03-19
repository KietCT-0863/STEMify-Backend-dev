using System.Reflection;
using FluentValidation;
using Infrastructure.Idempotency;
using Infrastructure.Resilience;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Common.Configurations;
using Notification.Application.Common.Hubs;

namespace Notification.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddMediatR(config =>
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
            );

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddScoped<IIdempotencyService, InMemoryIdempotencyService>();
            services.AddScoped<IPollyResilienceService, PollyResilienceService>();

            services.AddGrpc();

            services.AddSignalR();

            services.Configure<ClientAppSettings>(configuration.GetSection("ClientApp"));
            services.Configure<SendGridSettings>(configuration.GetSection("SendGridSettings"));

            return services;
        }
    }
}
