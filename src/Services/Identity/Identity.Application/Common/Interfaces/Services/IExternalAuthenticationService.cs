using Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Common.Interfaces.Services;

/// <summary>
/// Interface for external authentication services (Google)
/// </summary>
public interface IExternalAuthenticationService
{
    string ProviderName { get; }

    ExternalAuthProvider ProviderType { get; }
    Task<bool> ValidateExternalLoginAsync(ExternalLoginInfo externalLoginInfo);

    Task<(string Email, string FirstName, string LastName)> ExtractUserInfoAsync(
        ExternalLoginInfo externalLoginInfo
    );

    Task<Dictionary<string, string>> GetAdditionalClaimsAsync(ExternalLoginInfo externalLoginInfo);
}
