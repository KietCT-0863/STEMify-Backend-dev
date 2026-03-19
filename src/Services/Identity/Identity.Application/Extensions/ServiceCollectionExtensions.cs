using System.Reflection;
using Common.Logging;
using FluentValidation;
using Identity.Application.Common.Behaviours;
using Identity.Application.Common.Interfaces;
using Identity.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application.Extensions;

/// <summary>
/// Application layer service registration
/// Following clean architecture - application services registered here
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register MediatR Behaviors (Order matters!)
        // Use Common.Logging LoggingBehavior instead of local implementation
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IStreamPipelineBehavior<,>), typeof(StreamLoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));

        // Register FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register Domain Services
        services.AddScoped<IUserDomainService, UserDomainService>();

        // Register application services
        services.AddScoped<IDataSeeder, DataSeederService>();
        services.AddScoped<JwtOrganizationClaimsBuilder>();

        return services;
    }
}
