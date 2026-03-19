namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Application interface for OpenIddict configuration operations
/// Clean Architecture compliant - interface in Application layer
/// </summary>
public interface IOpenIddictConfigurationService
{
    /// <summary>
    /// Ensure required OAuth scopes exist in the system
    /// </summary>
    Task<bool> EnsureScopesExistAsync();

    /// <summary>
    /// Ensure required OAuth applications exist in the system
    /// </summary>
    Task<bool> EnsureApplicationsExistAsync();

    /// <summary>
    /// Check if clients should be forcefully recreated
    /// </summary>
    bool ShouldForceRecreateClients();

    /// <summary>
    /// Get API client secret for configuration
    /// </summary>
    string GetApiClientSecret();
}

/// <summary>
/// Configuration data for OpenIddict client setup
/// </summary>
public class OpenIddictClientConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public List<string> RedirectUris { get; set; } = new();
    public List<string> PostLogoutRedirectUris { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
    public bool ForceRecreate { get; set; }
}
