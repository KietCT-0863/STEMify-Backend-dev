using Cart.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cart.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            // Add MediatR
            services.AddMediatR(config => config.RegisterServicesFromAssembly(currentAssembly));

            // add MediatR Behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Add fluent validation
            services.AddValidatorsFromAssembly(currentAssembly);

            return services;
        }
    }
}
