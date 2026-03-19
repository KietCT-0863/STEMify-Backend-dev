using Contracts.Abstractions.Services;
using FluentValidation;
using Infrastructure.Abstractions.Services.Cloudinary;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resource.Application.Common.Behaviors;
using System.Reflection;

namespace Resource.Application.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration
        )
        {
            services.AddScoped<ICloudinaryService, CloudinaryService>();

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
