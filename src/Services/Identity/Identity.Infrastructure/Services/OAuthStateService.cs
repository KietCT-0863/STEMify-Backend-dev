using System.Text.Json;
using Identity.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

public class OAuthStateService : IOAuthStateService
{
    private readonly IDataProtector _protector;
    private readonly ILogger<OAuthStateService> _logger;
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(5);

    public OAuthStateService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<OAuthStateService> logger)
    {
        _protector = dataProtectionProvider.CreateProtector("Identity.OAuth.State.v1");
        _logger = logger;
    }

    public string CreateState(string invitationToken, string codeVerifier)
    {
        try
        {
            var stateData = new OAuthStateData
            {
                InvitationToken = invitationToken,
                CodeVerifier = codeVerifier,
                Nonce = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTimeOffset.UtcNow.Add(StateLifetime)
            };

            var json = JsonSerializer.Serialize(stateData);
            var protectedState = _protector.Protect(json);

            // Convert to Base64URL for safe URL transmission
            var base64UrlState = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(protectedState))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            _logger.LogDebug("Created OAuth state for invitation token {Token} with nonce {Nonce}",
                invitationToken, stateData.Nonce);

            return base64UrlState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OAuth state for invitation token {Token}",
                invitationToken);
            throw;
        }
    }

    public bool ValidateState(string encryptedState, out string invitationToken, out string codeVerifier)
    {
        invitationToken = string.Empty;
        codeVerifier = string.Empty;

        try
        {
            // Convert from Base64URL back to normal Base64
            var base64 = encryptedState
                .Replace('-', '+')
                .Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var protectedState = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var json = _protector.Unprotect(protectedState);

            var stateData = JsonSerializer.Deserialize<OAuthStateData>(json);

            if (stateData == null)
            {
                _logger.LogWarning("OAuth state deserialization returned null");
                return false;
            }

            // Check if state has expired
            if (stateData.ExpiresAt < DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("OAuth state expired at {ExpiresAt}, nonce: {Nonce}",
                    stateData.ExpiresAt, stateData.Nonce);
                return false;
            }

            invitationToken = stateData.InvitationToken;
            codeVerifier = stateData.CodeVerifier;

            _logger.LogDebug("Validated OAuth state for invitation token {Token} with nonce {Nonce}",
                invitationToken, stateData.Nonce);

            return true;
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            _logger.LogError(ex, "Unexpected error validating OAuth state");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating OAuth state");
            return false;
        }
    }

    private class OAuthStateData
    {
        public string InvitationToken { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
