using Identity.Application.Common.Interfaces;
using Identity.Domain.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;

namespace Identity.Infrastructure.Data.Seeders;

/// <summary>
/// Infrastructure implementation for OAuth/OpenIddict seeding
/// Implements Application layer interface, uses Domain constants
/// </summary>
public class OAuthSeeder : IOAuthSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OAuthSeeder> _logger;

    public int Order => 3; // OAuth configuration seeded after users and roles

    public OAuthSeeder(IServiceProvider serviceProvider, ILogger<OAuthSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedOAuthConfigurationAsync(cancellationToken);
    }

    public async Task SeedOAuthConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding OAuth/OpenIddict configuration...");

        try
        {
            await SeedScopesAsync(cancellationToken);
            await SeedApplicationsAsync(cancellationToken);

            _logger.LogInformation("OAuth configuration seeding completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed OAuth configuration: {Message}", ex.Message);
            throw;
        }
    }

    private async Task SeedScopesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding OAuth scopes...");

        var scopeManager = _serviceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // Use domain constants for scopes
        var scopes = GetDefaultScopes();

        foreach (var scope in scopes)
        {
            var existingScope = await scopeManager.FindByNameAsync(scope.Name, cancellationToken);
            if (existingScope == null)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scope.Name,
                    DisplayName = scope.DisplayName,
                };

                foreach (var resource in scope.Resources)
                {
                    descriptor.Resources.Add(resource);
                }

                await scopeManager.CreateAsync(descriptor, cancellationToken);
                _logger.LogInformation("Created OAuth scope: {ScopeName}", scope.Name);
            }
            else
            {
                _logger.LogInformation(
                    "OAuth scope {ScopeName} already exists, skipping",
                    scope.Name
                );
            }
        }
    }

    private async Task SeedApplicationsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding OAuth applications...");

        var applicationManager =
            _serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Check if we should force recreate clients
        var configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        var forceRecreate = configuration.GetValue<bool>("OpenIddict:ForceRecreateClients");
        
        if (forceRecreate)
        {
            _logger.LogWarning("OpenIddict:ForceRecreateClients is enabled. Will update existing applications.");
        }

        // Use domain constants for applications
        var applications = SeedDataConstants.OAuth.DefaultApplications;

        foreach (var appData in applications)
        {
            var existingApp = await applicationManager.FindByClientIdAsync(
                appData.ClientId,
                cancellationToken
            );
            if (existingApp == null)
            {
                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = appData.ClientId,
                    DisplayName = appData.Name,
                    ApplicationType =
                        appData.Type == "public"
                            ? OpenIddictConstants.ClientTypes.Public
                            : OpenIddictConstants.ClientTypes.Confidential,
                    ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
                };

                // Add redirect URIs
                foreach (var uri in appData.RedirectUris)
                {
                    descriptor.RedirectUris.Add(new Uri(uri));
                }

                // Add post-logout redirect URIs
                foreach (var uri in appData.PostLogoutRedirectUris)
                {
                    descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                }

                // Set permissions for public clients
                SetPublicClientPermissions(descriptor, appData);

                await applicationManager.CreateAsync(descriptor, cancellationToken);
                _logger.LogInformation(
                    "Created OAuth application: {ApplicationName} ({ClientId})",
                    appData.Name,
                    appData.ClientId
                );
            }
            else
            {
                // Update existing application
                var descriptor = new OpenIddictApplicationDescriptor();
                await applicationManager.PopulateAsync(descriptor, existingApp, cancellationToken);

                // Clear and re-add redirect URIs
                descriptor.RedirectUris.Clear();
                foreach (var uri in appData.RedirectUris)
                {
                    descriptor.RedirectUris.Add(new Uri(uri));
                }

                // Clear and re-add post-logout redirect URIs
                descriptor.PostLogoutRedirectUris.Clear();
                foreach (var uri in appData.PostLogoutRedirectUris)
                {
                    descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                }

                // Update permissions for public clients
                if (appData.Type == "public")
                {
                    SetPublicClientPermissions(descriptor, appData);
                }

                await applicationManager.UpdateAsync(existingApp, descriptor, cancellationToken);
                _logger.LogInformation(
                    "Updated OAuth application: {ApplicationName} ({ClientId}) with new redirect URIs",
                    appData.Name,
                    appData.ClientId
                );
            }
        }
    }

    private static void SetPublicClientPermissions(
        OpenIddictApplicationDescriptor descriptor,
        ApplicationSeedData appData
    )
    {
        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.Endpoints.Authorization
        );
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Revocation);

        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode
        );
        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken
        );
        descriptor.Permissions.Add(
            OpenIddictConstants.Permissions.GrantTypes.Password
        );

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

        // Add scope permissions
        foreach (var scope in SeedDataConstants.OAuth.DefaultScopes)
        {
            descriptor.Permissions.Add(
                OpenIddictConstants.Permissions.Prefixes.Scope + scope
            );
        }
    }

    private static IEnumerable<ScopeDescriptor> GetDefaultScopes()
    {
        return new[]
        {
            // Standard OIDC scopes
            new ScopeDescriptor("openid", "OpenID Connect", Array.Empty<string>()),
            new ScopeDescriptor("profile", "User Profile", Array.Empty<string>()),
            new ScopeDescriptor("email", "Email Address", Array.Empty<string>()),
            new ScopeDescriptor("roles", "User Roles", Array.Empty<string>()),
            new ScopeDescriptor("offline_access", "Offline Access", Array.Empty<string>()),
            // Custom API scopes for STEMify
            new ScopeDescriptor(
                "stemify_api",
                "STEMify API Access",
                new[] { "stemify-web", "stemify-mobile" }
            ),
            new ScopeDescriptor(
                "classroom_api",
                "Classroom API Access",
                new[] { "stemify-web", "stemify-mobile" }
            ),
            new ScopeDescriptor(
                "resource_api",
                "Resource API Access",
                new[] { "stemify-web", "stemify-mobile" }
            ),
        };
    }

    private record ScopeDescriptor(string Name, string DisplayName, string[] Resources);
}
