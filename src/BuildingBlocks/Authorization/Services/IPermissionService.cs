namespace BuildingBlocks.Authorization.Services;

/// <summary>
/// Service for checking organization-level permissions
/// Provides caching for performance optimization
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        int organizationId,
        int subscriptionId,
        string permission);
    Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        int organizationId,
        int subscriptionId);
    Task<HashSet<string>> GetUserPermissionsInOrganizationAsync(
        Guid userId,
        int organizationId);

    Task<bool> HasPermissionInOrganizationAsync(
        Guid userId,
        int organizationId,
        string permission);

    void InvalidateUserCache(Guid userId, int organizationId);

    void ClearCache();
}
