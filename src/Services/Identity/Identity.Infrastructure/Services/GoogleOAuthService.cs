using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Identity.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Google OAuth 2.0 service with PKCE support (RFC 7636)
/// Handles manual OAuth flow without ASP.NET Identity middleware
/// </summary>
public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly HttpClient _httpClient;

    private const string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleJwksEndpoint = "https://www.googleapis.com/oauth2/v3/certs";

    public GoogleOAuthService(
        IConfiguration configuration,
        ILogger<GoogleOAuthService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GoogleOAuth");
    }

    public (string CodeVerifier, string CodeChallenge) GeneratePKCEChallenge()
    {
        // Generate code_verifier: random 43-128 character string
        var codeVerifier = GenerateRandomString(128);

        // Generate code_challenge: BASE64URL(SHA256(code_verifier))
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        _logger.LogDebug("Generated PKCE challenge");
        return (codeVerifier, codeChallenge);
    }

    public string BuildAuthorizationUrl(string state, string codeChallenge)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var redirectUri = _configuration["Authentication:Google:RedirectUri"]
            ?? $"{_configuration["OpenIddict:Authority"]}/api/auth/google/callback";

        if (string.IsNullOrEmpty(clientId))
        {
            throw new InvalidOperationException("Google OAuth ClientId not configured");
        }

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = "offline",
            ["prompt"] = "consent"
        };

        var queryString = string.Join("&", parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var authUrl = $"{GoogleAuthEndpoint}?{queryString}";

        _logger.LogInformation("Built Google OAuth authorization URL with PKCE");
        return authUrl;
    }

    public async Task<(string IdToken, string AccessToken)> ExchangeCodeForTokensAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        var redirectUri = _configuration["Authentication:Google:RedirectUri"]
            ?? $"{_configuration["OpenIddict:Authority"]}/api/auth/google/callback";

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google OAuth credentials not configured");
        }

        var parameters = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };

        try
        {
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(GoogleTokenEndpoint, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for tokens. Status: {Status}, Body: {Body}",
                    response.StatusCode, responseBody);
                throw new InvalidOperationException($"Failed to exchange authorization code: {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            var idToken = tokenResponse.GetProperty("id_token").GetString();
            var accessToken = tokenResponse.GetProperty("access_token").GetString();

            if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Token response missing id_token or access_token");
            }

            _logger.LogInformation("Successfully exchanged authorization code for tokens");
            return (idToken, accessToken);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error exchanging authorization code for tokens");
            throw new InvalidOperationException("Failed to exchange authorization code", ex);
        }
    }

    public async Task<Dictionary<string, string>> VerifyIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Split JWT into parts
            var parts = idToken.Split('.');
            if (parts.Length != 3)
            {
                throw new InvalidOperationException("Invalid JWT format");
            }

            // Decode payload (second part)
            var payload = parts[1];
            var payloadBytes = Base64UrlDecode(payload);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);

            var claims = JsonSerializer.Deserialize<JsonElement>(payloadJson);

            // Validate issuer
            var issuer = claims.GetProperty("iss").GetString();
            if (issuer != "https://accounts.google.com" && issuer != "accounts.google.com")
            {
                throw new InvalidOperationException($"Invalid issuer: {issuer}");
            }

            // Validate audience (client ID)
            var clientId = _configuration["Authentication:Google:ClientId"];
            var audience = claims.GetProperty("aud").GetString();
            if (audience != clientId)
            {
                throw new InvalidOperationException($"Invalid audience: {audience}");
            }

            // Validate expiry
            var exp = claims.GetProperty("exp").GetInt64();
            var expiryTime = DateTimeOffset.FromUnixTimeSeconds(exp);
            if (expiryTime < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("ID token has expired");
            }

            // Extract all claims to dictionary
            var claimsDictionary = new Dictionary<string, string>();
            foreach (var property in claims.EnumerateObject())
            {
                claimsDictionary[property.Name] = property.Value.ToString();
            }

            _logger.LogInformation("Successfully verified ID token for subject {Subject}",
                claimsDictionary.GetValueOrDefault("sub"));

            return claimsDictionary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify ID token");
            throw new InvalidOperationException("ID token verification failed", ex);
        }
    }

    public (string GoogleId, string Email, string FirstName, string LastName, string ProfilePictureUrl)
        ExtractUserInfo(Dictionary<string, string> claims)
    {
        var googleId = claims.GetValueOrDefault("sub") ?? string.Empty;
        var email = claims.GetValueOrDefault("email") ?? string.Empty;
        var givenName = claims.GetValueOrDefault("given_name") ?? string.Empty;
        var familyName = claims.GetValueOrDefault("family_name") ?? string.Empty;
        var picture = claims.GetValueOrDefault("picture") ?? string.Empty;

        // Fallback: split name if given_name/family_name not present
        if (string.IsNullOrEmpty(givenName) && string.IsNullOrEmpty(familyName))
        {
            var name = claims.GetValueOrDefault("name") ?? string.Empty;
            var nameParts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            givenName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            familyName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }

        _logger.LogInformation("Extracted user info for {Email}", email);

        return (googleId, email, givenName, familyName, picture);
    }

    #region Helper Methods

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var sb = new StringBuilder(length);
        foreach (var b in randomBytes)
        {
            sb.Append(chars[b % chars.Length]);
        }

        return sb.ToString();
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(challengeBytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    #endregion
}
