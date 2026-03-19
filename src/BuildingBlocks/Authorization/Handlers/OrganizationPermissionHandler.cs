using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Authorization.Requirements;
using BuildingBlocks.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Authorization.Handlers;

/// <summary>
/// Authorization handler that validates organization-scoped permissions
/// based on JWT token claims and request headers.
/// </summary>
public class OrganizationPermissionHandler : AuthorizationHandler<OrganizationPermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<OrganizationPermissionHandler> _logger;

    public OrganizationPermissionHandler(
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService,
        ILogger<OrganizationPermissionHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationPermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null, cannot validate organization permission");
            context.Fail();
            return;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated");
            context.Fail();
            return;
        }

        // Extract user ID from claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID claim not found or invalid");
            context.Fail();
            return;
        }

        // Extract organization and subscription context from headers
        var organizationContext = ExtractOrganizationContext(httpContext, requirement);
        if (organizationContext == null)
        {
            context.Fail();
            return;
        }

        var (organizationId, subscriptionId) = organizationContext.Value;

        // Extract organizations claim from JWT
        var organizationsClaim = user.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
        {
            _logger.LogWarning(
                "User {UserId} has no organizations claim",
                userId);
            context.Fail();
            return;
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
            context.Fail();
            return;
        }

        if (organizations == null || !organizations.Any())
        {
            _logger.LogWarning("User {UserId} has no organizations", userId);
            context.Fail();
            return;
        }

        // Find the active organization
        var organization = organizations.FirstOrDefault(o => o.OrganizationId == organizationId);
        if (organization == null)
        {
            _logger.LogWarning(
                "User {UserId} does not belong to organization {OrganizationId}",
                userId, organizationId);
            context.Fail();
            return;
        }

        // Find the active subscription
        var subscription = organization.Subscriptions?.FirstOrDefault(s => s.SubscriptionId == subscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning(
                "User {UserId} does not have subscription {SubscriptionId} in organization {OrganizationId}",
                userId, subscriptionId, organizationId);
            context.Fail();
            return;
        }

        if (!subscription.IsActive)
        {
            _logger.LogWarning(
                "User {UserId} subscription {SubscriptionId} is inactive",
                userId, subscriptionId);
            context.Fail();
            return;
        }

        // Check if user has the required permission via their role
        var hasPermission = await _permissionService.HasPermissionAsync(
            userId,
            organizationId,
            subscriptionId,
            requirement.Permission);

        if (hasPermission)
        {
            _logger.LogInformation(
                "User {UserId} with role {Role} granted permission {Permission} in org {OrganizationId} sub {SubscriptionId}",
                userId, subscription.Role, requirement.Permission, organizationId, subscriptionId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} with role {Role} denied permission {Permission} in org {OrganizationId} sub {SubscriptionId}",
                userId, subscription.Role, requirement.Permission, organizationId, subscriptionId);
            context.Fail();
        }
    }

    private (int OrganizationId, int SubscriptionId)? ExtractOrganizationContext(
        HttpContext httpContext,
        OrganizationPermissionRequirement requirement)
    {
        // Extract from headers
        var organizationHeader = httpContext.Request.Headers["X-Active-Organization"].FirstOrDefault();
        var subscriptionHeader = httpContext.Request.Headers["X-Active-Subscription"].FirstOrDefault();

        if (requirement.RequireOrganizationContext && string.IsNullOrEmpty(organizationHeader))
        {
            _logger.LogWarning("X-Active-Organization header is required but not provided");
            return null;
        }

        if (requirement.RequireSubscriptionContext && string.IsNullOrEmpty(subscriptionHeader))
        {
            _logger.LogWarning("X-Active-Subscription header is required but not provided");
            return null;
        }

        // Try parsing organization ID
        if (!int.TryParse(organizationHeader, out var organizationId))
        {
            // Try extracting from route
            if (httpContext.Request.RouteValues.TryGetValue("organizationId", out var orgRouteValue) &&
                int.TryParse(orgRouteValue?.ToString(), out organizationId))
            {
                _logger.LogDebug("Extracted organizationId {OrganizationId} from route", organizationId);
            }
            else if (requirement.RequireOrganizationContext)
            {
                _logger.LogWarning("Invalid X-Active-Organization header value: {Value}", organizationHeader);
                return null;
            }
        }

        // Try parsing subscription ID
        if (!int.TryParse(subscriptionHeader, out var subscriptionId))
        {
            // Try extracting from route
            if (httpContext.Request.RouteValues.TryGetValue("subscriptionId", out var subRouteValue) &&
                int.TryParse(subRouteValue?.ToString(), out subscriptionId))
            {
                _logger.LogDebug("Extracted subscriptionId {SubscriptionId} from route", subscriptionId);
            }
            else if (requirement.RequireSubscriptionContext)
            {
                _logger.LogWarning("Invalid X-Active-Subscription header value: {Value}", subscriptionHeader);
                return null;
            }
        }

        return (organizationId, subscriptionId);
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
        
        public bool IsActive { get; set; }
    }
}
