using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Common.Interfaces;
using Payment.Infrastructure.Events;
using Payment.Infrastructure.Gateways;
using Payment.Infrastructure.Gateways.Settings;
using Payment.Infrastructure.Repositories;

namespace Payment.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Note: DbContext is registered in Program.cs using Aspire's AddNpgsqlDbContext
            // No need to register it here

            // Repositories
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Event Publishers
            services.AddScoped<IPaymentEventPublisher, PaymentEventPublisher>();

            // Payment Gateway Settings
            services.Configure<PayOSSettings>(configuration.GetSection("PaymentGateways:PayOS"));

            // Payment Gateways
            services.AddScoped<IPaymentGateway, PayOSGateway>();
            // services.AddScoped<IPaymentGateway, StripeGateway>();

            return services;
        }
    }
}
