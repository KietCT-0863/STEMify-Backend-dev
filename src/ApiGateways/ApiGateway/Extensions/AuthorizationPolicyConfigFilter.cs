using BuildingBlocks.Authorization.Extensions;
using BuildingBlocks.Authorization.Requirements;
using Yarp.ReverseProxy.Configuration;

namespace ApiGateway.Extensions
{
    public class AuthorizationPolicyConfigFilter : IProxyConfigFilter
    {
        public ValueTask<ClusterConfig> ConfigureClusterAsync(
            ClusterConfig cluster,
            CancellationToken cancel
        )
        {
            
            return new ValueTask<ClusterConfig>(cluster);
        }

        public ValueTask<RouteConfig> ConfigureRouteAsync(
            RouteConfig route,
            ClusterConfig? cluster,
            CancellationToken cancel
        )
        {
            if (
                route.Metadata != null
                && route.Metadata.TryGetValue("AuthorizationPolicy", out var policyName)
                && !string.IsNullOrEmpty(policyName)
            )
            {
                route = route with { AuthorizationPolicy = policyName };
            }

            if (
                route.Metadata != null
                && route.Metadata.TryGetValue("RequiredPermission", out var permission)
                && !string.IsNullOrEmpty(permission)
            )
            {
                var permissionPolicyName = $"RequirePermission:{permission}";
                route = route with { AuthorizationPolicy = permissionPolicyName };
            }

            return new ValueTask<RouteConfig>(route);
        }
    }
}
