using System.Security.Claims;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Google authentication service implementation
/// Implements Single Responsibility Principle - only handles Google-specific authentication logic
/// </summary>
public class GoogleAuthenticationService : IExternalAuthenticationService
{
    private readonly ILogger<GoogleAuthenticationService> _logger;

    public GoogleAuthenticationService(ILogger<GoogleAuthenticationService> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "Google";

    public ExternalAuthProvider ProviderType => ExternalAuthProvider.Google;

    public Task<bool> ValidateExternalLoginAsync(ExternalLoginInfo externalLoginInfo)
    {
        if (externalLoginInfo == null)
        {
            _logger.LogWarning("External login info is null");
            return Task.FromResult(false);
        }

        if (externalLoginInfo.LoginProvider != ProviderName)
        {
            _logger.LogWarning(
                "Expected provider {Expected} but got {Actual}",
                ProviderName,
                externalLoginInfo.LoginProvider
            );
            return Task.FromResult(false);
        }

        // Validate required claims
        var email = externalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Google login does not contain email claim");
            return Task.FromResult(false);
        }

        _logger.LogInformation("Google external login validated successfully for email {Email}", email);
        return Task.FromResult(true);
    }

    public Task<(string Email, string FirstName, string LastName)> ExtractUserInfoAsync(
        ExternalLoginInfo externalLoginInfo
    )
    {
        var principal = externalLoginInfo.Principal;

        // Extract email
        var email =
            principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
            ?? string.Empty;

        // Extract given name (first name)
        var givenName =
            principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")
            ?? string.Empty;

        // Extract surname (last name)
        var surname =
            principal.FindFirstValue(ClaimTypes.Surname)
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")
            ?? string.Empty;

        // If no separate first/last names, try to split the full name
        if (string.IsNullOrEmpty(givenName) && string.IsNullOrEmpty(surname))
        {
            var name =
                principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue("name")
                ?? string.Empty;

            if (!string.IsNullOrEmpty(name))
            {
                var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                givenName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                surname = nameParts.Length > 1 ? nameParts[1] : string.Empty;
            }
        }

        _logger.LogInformation(
            "Extracted user info from Google: Email={Email}, FirstName={FirstName}, LastName={LastName}",
            email,
            givenName,
            surname
        );

        return Task.FromResult((email, givenName, surname));
    }

    public Task<Dictionary<string, string>> GetAdditionalClaimsAsync(
        ExternalLoginInfo externalLoginInfo
    )
    {
        var principal = externalLoginInfo.Principal;
        var additionalClaims = new Dictionary<string, string>();

        // Extract profile picture URL
        var picture = principal.FindFirstValue("picture") ?? principal.FindFirstValue("urn:google:picture");
        if (!string.IsNullOrEmpty(picture))
        {
            additionalClaims["picture"] = picture;
        }

        // Extract locale
        var locale = principal.FindFirstValue("locale") ?? principal.FindFirstValue("urn:google:locale");
        if (!string.IsNullOrEmpty(locale))
        {
            additionalClaims["locale"] = locale;
        }

        // Extract email verified status
        var emailVerified = principal.FindFirstValue("email_verified");
        if (!string.IsNullOrEmpty(emailVerified))
        {
            additionalClaims["email_verified"] = emailVerified;
        }

        _logger.LogInformation(
            "Extracted {Count} additional claims from Google",
            additionalClaims.Count
        );

        return Task.FromResult(additionalClaims);
    }
}
