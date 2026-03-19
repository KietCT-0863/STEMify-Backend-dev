using BuildingBlocks.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Authorization.Extensions;

public static class AuthorizationPolicyExtensions
{
    private const string RequirePermissionPrefix = "RequirePermission:";

       public static IServiceCollection AddOrganizationAuthorization(
        this IServiceCollection services,
        Action<AuthorizationOptions>? configureOptions = null)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireClaim("platform_role", "Admin"));

            options.AddPolicy("StaffOnly", policy =>
                policy.RequireAssertion(context =>
                {
                    var role = context.User.FindFirst("platform_role")?.Value;
                    return role == "Admin" || role == "Staff";
                }));

            options.AddPolicy("MemberOnly", policy =>
                policy.RequireAuthenticatedUser());

            configureOptions?.Invoke(options);
        });

        return services;
    }

    public static void AddPermissionPolicy(
        this AuthorizationOptions options,
        string permission,
        bool requireOrganizationContext = true,
        bool requireSubscriptionContext = true)
    {
        var policyName = $"{RequirePermissionPrefix}{permission}";

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new OrganizationPermissionRequirement(
                permission,
                requireOrganizationContext,
                requireSubscriptionContext));
        });
    }

       public static void AddPermissionPolicies(
        this AuthorizationOptions options,
        IEnumerable<string> permissions)
    {
        foreach (var permission in permissions)
        {
            options.AddPermissionPolicy(permission);
        }
    }

    public static void AddAllOrganizationPermissionPolicies(this AuthorizationOptions options)
    {
        var permissions = typeof(Permissions.OrganizationPermissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => f.GetValue(null)?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        foreach (var permission in permissions)
        {
            if (!string.IsNullOrEmpty(permission))
            {
                options.AddPermissionPolicy(permission);
            }
        }
    }

   
    public static OrganizationPermissionRequirement RequirePermission(
        string permission,
        bool requireOrganizationContext = true,
        bool requireSubscriptionContext = true)
    {
        return new OrganizationPermissionRequirement(
            permission,
            requireOrganizationContext,
            requireSubscriptionContext);
    }
}
