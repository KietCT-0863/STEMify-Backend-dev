//using OpenIddict.Abstractions;
using System.Security.Claims;

namespace Identity.Application.Common.Interfaces;

public interface IOpenIddictService
{
    Task<object?> FindApplicationByClientIdAsync(string clientId);
    Task<object?> FindAuthorizationAsync(
        string subject,
        string client,
        string status,
        string type,
        IEnumerable<string> scopes
    );
    Task<string> GetApplicationConsentTypeAsync(object application);
    Task<ClaimsPrincipal> CreateApplicationPrincipalAsync(object application);
    Task<IEnumerable<object>> FindAuthorizationsAsync(
        string subject,
        string client,
        string status,
        string type,
        IEnumerable<string> scopes
    );
    Task<IEnumerable<string>> ListResourcesAsync(IEnumerable<string> scopes);
}
