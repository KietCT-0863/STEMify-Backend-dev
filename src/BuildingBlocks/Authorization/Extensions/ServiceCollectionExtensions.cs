using BuildingBlocks.Authorization.Handlers;
using BuildingBlocks.Authorization.Helpers;
using BuildingBlocks.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authorization.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationPermissions(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationHandler, OrganizationPermissionHandler>();
        services.AddScoped<IGrpcContextService, GrpcContextService>();

        services.AddScoped<OrganizationContext>();

        return services;
    }
}
