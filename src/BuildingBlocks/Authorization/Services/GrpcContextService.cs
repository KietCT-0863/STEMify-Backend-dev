using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Authorization.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authorization.Services;

/// <summary>
/// Service for extracting and forwarding organization context and permission claims in gRPC calls
/// </summary>
public class GrpcContextService : IGrpcContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<GrpcContextService> _logger;

    // Metadata keys for gRPC calls
    private const string MetadataKeyOrganizationId = "x-organization-id";
    private const string MetadataKeySubscriptionId = "x-subscription-id";
    private const string MetadataKeyUserId = "x-user-id";
    private const string MetadataKeyPermissions = "x-permissions";
    private const string MetadataKeyRole = "x-role";

    public GrpcContextService(
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService,
        ILogger<GrpcContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }

    public Dictionary<string, string> GetGrpcMetadata()
    {
        var context = GetOrganizationContext();
        if (context == null)
        {
            _logger.LogWarning("Cannot create gRPC metadata: organization context not found");
            return new Dictionary<string, string>();
        }

        var (organizationId, subscriptionId) = context.Value;

        // Get permissions synchronously from JWT
        var permissions = GetPermissionsFromJwt(organizationId, subscriptionId);

        return GetGrpcMetadata(organizationId, subscriptionId, permissions);
    }

    public Dictionary<string, string> GetGrpcMetadata(
        int organizationId,
        int subscriptionId,
        IEnumerable<string> permissions)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext?.User?.FindFirst("sub")?.Value;

        var metadata = new Dictionary<string, string>();

        // Add organization context
        metadata[MetadataKeyOrganizationId] = organizationId.ToString();
        metadata[MetadataKeySubscriptionId] = subscriptionId.ToString();

        if (!string.IsNullOrEmpty(userId))
        {
            metadata[MetadataKeyUserId] = userId;
        }

        // Add permissions as comma-separated string
        var permissionsList = permissions.ToList();
        if (permissionsList.Any())
        {
            metadata[MetadataKeyPermissions] = string.Join(",", permissionsList);
        }

        // Add role from subscription
        var role = GetRoleFromJwt(organizationId, subscriptionId);
        if (!string.IsNullOrEmpty(role))
        {
            metadata[MetadataKeyRole] = role;
        }

        _logger.LogDebug(
            "Created gRPC metadata with context: OrgId={OrganizationId}, SubId={SubscriptionId}, Permissions={PermissionCount}",
            organizationId, subscriptionId, permissionsList.Count);

        return metadata;
    }

    public (int OrganizationId, int SubscriptionId)? GetOrganizationContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Try to get from headers first
        var organizationHeader = httpContext.Request.Headers["X-Active-Organization"].FirstOrDefault();
        var subscriptionHeader = httpContext.Request.Headers["X-Active-Subscription"].FirstOrDefault();

        if (int.TryParse(organizationHeader, out var organizationId) &&
            int.TryParse(subscriptionHeader, out var subscriptionId))
        {
            return (organizationId, subscriptionId);
        }

        // Try to get from route values
        if (httpContext.Request.RouteValues.TryGetValue("organizationId", out var orgRouteValue) &&
            httpContext.Request.RouteValues.TryGetValue("subscriptionId", out var subRouteValue))
        {
            if (int.TryParse(orgRouteValue?.ToString(), out organizationId) &&
                int.TryParse(subRouteValue?.ToString(), out subscriptionId))
            {
                return (organizationId, subscriptionId);
            }
        }

        return null;
    }

    public async Task<IEnumerable<string>> GetCurrentPermissionsAsync()
    {
        var context = GetOrganizationContext();
        if (context == null)
        {
            return Enumerable.Empty<string>();
        }

        var (organizationId, subscriptionId) = context.Value;
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier) 
            ?? httpContext?.User?.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Enumerable.Empty<string>();
        }

        var permissions = await _permissionService.GetUserPermissionsAsync(
            userId,
            organizationId,
            subscriptionId);

        return permissions;
    }

    /// <summary>
    /// Get permissions from JWT claim (synchronous fallback)
    /// </summary>
    private IEnumerable<string> GetPermissionsFromJwt(int organizationId, int subscriptionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return Enumerable.Empty<string>();
        }

        // Extract organizations claim from JWT
        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
        {
            return Enumerable.Empty<string>();
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
            _logger.LogError(ex, "Failed to parse organizations claim for gRPC context");
            return Enumerable.Empty<string>();
        }

        if (organizations == null || !organizations.Any())
        {
            return Enumerable.Empty<string>();
        }

        // Find the active organization
        var organization = organizations.FirstOrDefault(o => o.OrganizationId == organizationId);
        if (organization == null)
        {
            return Enumerable.Empty<string>();
        }

        // Find the active subscription
        var subscription = organization.Subscriptions?.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscription == null)
        {
            return Enumerable.Empty<string>();
        }

        // Get permissions for the role
        var role = subscription.Role;
        var permissions = RolePermissionsMapping.GetPermissionsForRole(role);

        return permissions;
    }

    /// <summary>
    /// Get role from JWT claim
    /// </summary>
    private string? GetRoleFromJwt(int organizationId, int subscriptionId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
        {
            return null;
        }

        try
        {
            var organizations = JsonSerializer.Deserialize<List<OrganizationData>>(
                organizationsClaim,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var organization = organizations?.FirstOrDefault(o => o.OrganizationId == organizationId);
            var subscription = organization?.Subscriptions?.FirstOrDefault(s => s.SubscriptionId == subscriptionId);

            return subscription?.Role;
        }
        catch
        {
            return null;
        }
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
}

