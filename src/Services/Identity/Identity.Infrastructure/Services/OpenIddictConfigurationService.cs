using Identity.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of OpenIddict configuration service
/// Contains actual OpenIddict API interactions
/// </summary>
public class OpenIddictConfigurationService : IOpenIddictConfigurationService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenIddictConfigurationService> _logger;

    public OpenIddictConfigurationService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IConfiguration configuration,
        ILogger<OpenIddictConfigurationService> logger
    )
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> EnsureScopesExistAsync()
    {
        try
        {
            _logger.LogInformation("Creating OpenIddict scopes...");

            if (await _scopeManager.FindByNameAsync("api") == null)
            {
                await _scopeManager.CreateAsync(
                    new OpenIddictScopeDescriptor
                    {
                        Name = "api",
                        DisplayName = "API Access",
                        Description = "Access to the API",
                        Resources = { "stemify-api" },
                    }
                );
                _logger.LogInformation("Created 'api' scope");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OpenIddict scopes");
            return false;
        }
    }

    public async Task<bool> EnsureApplicationsExistAsync()
    {
        try
        {
            var clientConfigs = GetClientConfigurations();

            foreach (var config in clientConfigs)
            {
                await EnsureClientExistsAsync(config);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OpenIddict applications");
            return false;
        }
    }

    public bool ShouldForceRecreateClients()
    {
        return _configuration.GetValue<bool>("OpenIddict:ForceRecreateClients");
    }

    public string GetApiClientSecret()
    {
        return _configuration["OpenIddict:ApiClientSecret"]
            ?? Environment.GetEnvironmentVariable("OPENIDDICT_API_SECRET")
            ?? throw new InvalidOperationException("API client secret not configured");
    }

    #region Private Methods

    private async Task EnsureClientExistsAsync(OpenIddictClientConfiguration config)
    {
        _logger.LogInformation("Processing client: {ClientId}", config.ClientId);

        var existingClient = await _applicationManager.FindByClientIdAsync(config.ClientId);

        if (existingClient != null && config.ForceRecreate)
        {
            _logger.LogInformation("Force recreating client: {ClientId}", config.ClientId);
            await _applicationManager.DeleteAsync(existingClient);
            existingClient = null;
        }

        if (existingClient == null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = config.ClientId,
                DisplayName = config.DisplayName,
                ConsentType = config.ConsentType,
                ClientType = config.ClientType,
            };

            if (!string.IsNullOrEmpty(config.ClientSecret))
            {
                descriptor.ClientSecret = config.ClientSecret;
            }

            foreach (var uri in config.RedirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(uri));
            }

            foreach (var uri in config.PostLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
            }

            foreach (var permission in config.Permissions)
            {
                descriptor.Permissions.Add(permission);
            }

            foreach (var requirement in config.Requirements)
            {
                descriptor.Requirements.Add(requirement);
            }

            await _applicationManager.CreateAsync(descriptor);
            _logger.LogInformation("Created client: {ClientId}", config.ClientId);
        }
        else
        {
            _logger.LogInformation("Client already exists: {ClientId}", config.ClientId);
        }
    }

    private IEnumerable<OpenIddictClientConfiguration> GetClientConfigurations()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var forceRecreate = false;
        if (environment == "Development")
        {
            forceRecreate = ShouldForceRecreateClients();
        }

        return new[]
        {
            // 1) Swagger UI: chỉ dùng để test, public client, PKCE cần thiết
            new OpenIddictClientConfiguration
            {
                ClientId = "swagger-ui",
                DisplayName = "Swagger UI",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                ForceRecreate = forceRecreate,
                RedirectUris =
                {
                    "http://localhost:3000/swagger/oauth2-redirect.html",
                    "https://localhost:7002/swagger/oauth2-redirect.html",
                },
                Permissions =
                {
                    // Cho phép các endpoint cơ bản
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    // Grant type PKCE
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    // Các scope được dùng trong API
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            },
            // 2) Frontend App (Next.js) chạy ở localhost:3000
            new OpenIddictClientConfiguration
            {
                ClientId = "my-nextjs-client",
                DisplayName = "My Next.js App",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                ForceRecreate = forceRecreate,
                RedirectUris = { "https://localhost:3000/api/auth/callback/oidc" },
                PostLogoutRedirectUris = { "https://localhost:3000/api/auth/logout-oidc" },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    // PKCE flow
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    // Scope
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "api",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            },
            // 3) API Client (server-to-server)
            new OpenIddictClientConfiguration
            {
                ClientId = "my-api-client",
                DisplayName = "My API Client",
                ClientSecret = GetApiClientSecret(),
                ClientType = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "api",
                },
            },
        };
    }

    #endregion
}
