using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Authorization.Helpers;

public class OrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrganizationContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? GetCurrentOrganizationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try header first
        var organizationHeader = httpContext.Request.Headers["X-Active-Organization"].FirstOrDefault();
        if (int.TryParse(organizationHeader, out var organizationId))
            return organizationId;

        // Try route values
        if (httpContext.Request.RouteValues.TryGetValue("organizationId", out var orgRouteValue) &&
            int.TryParse(orgRouteValue?.ToString(), out organizationId))
            return organizationId;

        return null;
    }

    public int? GetCurrentSubscriptionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try header first
        var subscriptionHeader = httpContext.Request.Headers["X-Active-Subscription"].FirstOrDefault();
        if (int.TryParse(subscriptionHeader, out var subscriptionId))
            return subscriptionId;

        // Try route values
        if (httpContext.Request.RouteValues.TryGetValue("subscriptionId", out var subRouteValue) &&
            int.TryParse(subRouteValue?.ToString(), out subscriptionId))
            return subscriptionId;

        return null;
    }

    public Guid? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier) ??
                         httpContext.User.FindFirst("sub");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }

    public string? GetCurrentOrganizationRole()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var organizationId = GetCurrentOrganizationId();
        var subscriptionId = GetCurrentSubscriptionId();

        if (!organizationId.HasValue || !subscriptionId.HasValue)
            return null;

        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
            return null;

        try
        {
            var organizations = JsonSerializer.Deserialize<List<OrganizationData>>(
                organizationsClaim,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var organization = organizations?.FirstOrDefault(o => o.OrganizationId == organizationId.Value);
            var subscription = organization?.Subscriptions?.FirstOrDefault(s => s.SubscriptionId == subscriptionId.Value);

            return subscription?.Role;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string? GetPlatformRole()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.FindFirst("platform_role")?.Value;
    }

    public bool IsPlatformAdmin()
    {
        return GetPlatformRole() == "Admin";
    }

    public List<OrganizationData> GetUserOrganizations()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return new List<OrganizationData>();

        var organizationsClaim = httpContext.User.FindFirst("organizations")?.Value;
        if (string.IsNullOrEmpty(organizationsClaim))
            return new List<OrganizationData>();

        try
        {
            return JsonSerializer.Deserialize<List<OrganizationData>>(
                organizationsClaim,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<OrganizationData>();
        }
        catch (JsonException)
        {
            return new List<OrganizationData>();
        }
    }

    public class OrganizationData
    {
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public List<SubscriptionData>? Subscriptions { get; set; }
    }

    public class SubscriptionData
    {
        public int SubscriptionId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }
}
