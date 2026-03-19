using System.Collections.Immutable;
using System.Security.Claims;
using Identity.Application.Common.Interfaces;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Services;

public class OpenIddictService : IOpenIddictService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;

    public OpenIddictService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager
    )
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
    }

    public async Task<object?> FindApplicationByClientIdAsync(string clientId)
    {
        return await _applicationManager.FindByClientIdAsync(clientId);
    }

    public async Task<object?> FindAuthorizationAsync(
        string subject,
        string client,
        string status,
        string type,
        IEnumerable<string> scopes
    )
    {
        await foreach (
            var authorization in _authorizationManager.FindAsync(
                subject: subject,
                client: client,
                status: status,
                type: type,
                scopes: scopes.ToImmutableArray()
            )
        )
        {
            return authorization;
        }
        return null;
    }

    public async Task<string> GetApplicationConsentTypeAsync(object application)
    {
        return await _applicationManager.GetConsentTypeAsync(application) ?? string.Empty;
    }

    public async Task<ClaimsPrincipal> CreateApplicationPrincipalAsync(object application)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role
        );

        // Use the client_id as the subject identifier.
        identity.SetClaim(Claims.Subject, await _applicationManager.GetClientIdAsync(application));
        identity.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));

        identity.SetDestinations(static claim =>
            claim.Type switch
            {
                Claims.Name or Claims.Subject => ImmutableArray.Create(Destinations.AccessToken),
                _ => ImmutableArray<string>.Empty,
            }
        );

        return new ClaimsPrincipal(identity);
    }

    public async Task<IEnumerable<object>> FindAuthorizationsAsync(
        string subject,
        string client,
        string status,
        string type,
        IEnumerable<string> scopes
    )
    {
        var authorizations = new List<object>();
        await foreach (
            var authorization in _authorizationManager.FindAsync(
                subject: subject,
                client: client,
                status: status,
                type: type,
                scopes: scopes.ToImmutableArray()
            )
        )
        {
            authorizations.Add(authorization);
        }
        return authorizations;
    }

    public async Task<IEnumerable<string>> ListResourcesAsync(IEnumerable<string> scopes)
    {
        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(scopes.ToImmutableArray()))
        {
            resources.Add(resource);
        }
        return resources;
    }
}
