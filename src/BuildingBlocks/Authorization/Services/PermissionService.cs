using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Authorization.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authorization.Services;

public class PermissionService : IPermissionService
{
    private readonly IMemoryCache _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PermissionService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    private const string UserSubscriptionPermissionsCacheKey = "permissions:user:{0}:org:{1}:sub:{2}";
    private const string UserOrganizationPermissionsCacheKey = "permissions:user:{0}:org:{1}";

    public PermissionService(
        IMemoryCache cache,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PermissionService> logger)
    {
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        int organizationId,
        int subscriptionId,
        string permission)
    {
        var permissions = await GetUserPermissionsAsync(userId, organizationId, subscriptionId);
        return permissions.Contains(permission);
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        int organizationId,
        int subscriptionId)
    {
        var cacheKey = string.Format(UserSubscriptionPermissionsCacheKey, userId, organizationId, subscriptionId);

        if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var cachedPermissions))
        {
            return cachedPermissions!;
        }


        var permissions = await ComputePermissionsAsync(userId, organizationId, subscriptionId);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheExpiration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(cacheKey, permissions, cacheOptions);

        return permissions;
    }

    public async Task<HashSet<string>> GetUserPermissionsInOrganizationAsync(
        Guid userId,
        int organizationId)
    {
        var cacheKey = string.Format(UserOrganizationPermissionsCacheKey, userId, organizationId);

        if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var cachedPermissions))
        {
            return cachedPermissions!;
        }

        var allPermissions = await ComputeOrganizationPermissionsAsync(userId, organizationId);

        // Cache the result
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(_cacheExpiration)
            .SetPriority(CacheItemPriority.Normal);

        _cache.Set(cacheKey, allPermissions, cacheOptions);

        return allPermissions;
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionInOrganizationAsync(
        Guid userId,
        int organizationId,
        string permission)
    {
        var permissions = await GetUserPermissionsInOrganizationAsync(userId, organizationId);
        return permissions.Contains(permission);
    }

    public void InvalidateUserCache(Guid userId, int organizationId)
    {
        var orgCacheKey = string.Format(UserOrganizationPermissionsCacheKey, userId, organizationId);
        _cache.Remove(orgCacheKey);
    }

    public void ClearCache()
    {
    }

    private async Task<HashSet<string>> ComputePermissionsAsync(
        Guid userId,
        int organizationId,
        int subscriptionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(
                "Cannot compute permissions: User not authenticated for userId {UserId}",
                userId);
            return new HashSet<string>();
        }

        // Extract organizations claim from JWT
        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
        {
            _logger.LogWarning(
                "User {UserId} has no organizations claim",
                userId);
            return new HashSet<string>();
        }

        // Parse organizations JSON
        List<OrganizationData>? organizations;
        try
        {
            organizations = JsonSerializer.Deserialize<List<OrganizationData>>(
                organizationsClaim,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse organizations claim for user {UserId}", userId);
            return new HashSet<string>();
        }

        if (organizations == null || !organizations.Any())
        {
            _logger.LogWarning("User {UserId} has no organizations", userId);
            return new HashSet<string>();
        }

        // Find the active organization
        var organization = organizations.FirstOrDefault(o => o.OrganizationId == organizationId);
        if (organization == null)
        {
            _logger.LogWarning(
                "User {UserId} does not belong to organization {OrganizationId}",
                userId, organizationId);
            return new HashSet<string>();
        }

        // Find the active subscription
        var subscription = organization.Subscriptions?.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning(
                "User {UserId} does not have subscription {SubscriptionId} in organization {OrganizationId}",
                userId, subscriptionId, organizationId);
            return new HashSet<string>();
        }

        if (!subscription.IsActive)
        {
            _logger.LogWarning(
                "User {UserId} subscription {SubscriptionId} is inactive",
                userId, subscriptionId);
            return new HashSet<string>();
        }

        // Map role to permissions
        var role = subscription.Role;
        var permissions = RolePermissionsMapping.GetPermissionsForRole(role);

        _logger.LogDebug(
            "Computed {Count} permissions for user {UserId} with role {Role} in org {OrganizationId} sub {SubscriptionId}",
            permissions.Count, userId, role, organizationId, subscriptionId);

        await Task.CompletedTask;
        return permissions;
    }

    // DTOs for deserializing organizations claim
    private class OrganizationData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int OrganizationId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("subscriptions")]
        public List<SubscriptionData>? Subscriptions { get; set; }
    }

    private class SubscriptionData
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int SubscriptionId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("properties")]
        public Dictionary<string, object>? Properties { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Compute permissions across all user's subscriptions in an organization
    /// </summary>
    private async Task<HashSet<string>> ComputeOrganizationPermissionsAsync(
        Guid userId,
        int organizationId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(
                "Cannot compute organization permissions: User not authenticated for userId {UserId}",
                userId);
            return new HashSet<string>();
        }

        // Extract organizations claim from JWT
        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
        {
            return new HashSet<string>();
        }

        // Parse organizations JSON
        List<OrganizationData>? organizations;
        try
        {
            organizations = JsonSerializer.Deserialize<List<OrganizationData>>(
                organizationsClaim,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse organizations claim for user {UserId}", userId);
            return new HashSet<string>();
        }

        if (organizations == null || !organizations.Any())
        {
            _logger.LogWarning("User {UserId} has no organizations", userId);
            return new HashSet<string>();
        }

        // Find the organization
        var organization = organizations.FirstOrDefault(o => o.OrganizationId == organizationId);
        if (organization == null)
        {
            return new HashSet<string>();
        }

        // Get all active subscriptions in this organization
        var activeSubscriptions = organization.Subscriptions?
            .Where(s => s.IsActive)
            .ToList() ?? new List<SubscriptionData>();

        if (!activeSubscriptions.Any())
        {
            return new HashSet<string>();
        }

        // Get all roles from active subscriptions
        var roles = activeSubscriptions
            .Select(s => s.Role)
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList();

        if (!roles.Any())
        {
            return new HashSet<string>();
        }

        // Combine permissions from all roles (union)
        var allPermissions = RolePermissionsMapping.GetPermissionsForRoles(roles);


        await Task.CompletedTask;
        return allPermissions;
    }
}
