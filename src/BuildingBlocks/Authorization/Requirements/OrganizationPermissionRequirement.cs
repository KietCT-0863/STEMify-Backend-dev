using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Authorization.Requirements;

public class OrganizationPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public bool RequireOrganizationContext { get; }

    public bool RequireSubscriptionContext { get; }

    public OrganizationPermissionRequirement(
        string permission,
        bool requireOrganizationContext = true,
        bool requireSubscriptionContext = true)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or empty", nameof(permission));

        Permission = permission;
        RequireOrganizationContext = requireOrganizationContext;
        RequireSubscriptionContext = requireSubscriptionContext;
    }
}
