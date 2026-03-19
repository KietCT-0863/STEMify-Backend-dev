namespace Identity.Application.Common.Interfaces.Services;

/// <summary>
/// Service for handling Google OAuth 2.0 flow with PKCE
/// Implements RFC 7636 (Proof Key for Code Exchange)
/// </summary>
public interface IGoogleOAuthService
{
    /// <summary>
    /// Generate PKCE code verifier and code challenge
    /// </summary>
    /// <returns>Tuple of (codeVerifier, codeChallenge)</returns>
    (string CodeVerifier, string CodeChallenge) GeneratePKCEChallenge();

    /// <summary>
    /// Build Google OAuth authorization URL with state and PKCE challenge
    /// </summary>
    /// <param name="state">OAuth state parameter (signed and encrypted)</param>
    /// <param name="codeChallenge">PKCE code challenge (S256)</param>
    /// <returns>Complete Google OAuth authorization URL</returns>
    string BuildAuthorizationUrl(string state, string codeChallenge);

    /// <summary>
    /// Exchange authorization code for ID token and access token
    /// </summary>
    /// <param name="code">Authorization code from Google callback</param>
    /// <param name="codeVerifier">PKCE code verifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (idToken, accessToken)</returns>
    Task<(string IdToken, string AccessToken)> ExchangeCodeForTokensAsync(
        string code,
        string codeVerifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify Google ID token signature and extract claims
    /// </summary>
    /// <param name="idToken">ID token from Google</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of claims from ID token</returns>
    Task<Dictionary<string, string>> VerifyIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract user information from verified ID token claims
    /// </summary>
    /// <param name="claims">Claims dictionary from verified ID token</param>
    /// <returns>Tuple of (googleId, email, firstName, lastName, profilePictureUrl)</returns>
    (string GoogleId, string Email, string FirstName, string LastName, string ProfilePictureUrl)
        ExtractUserInfo(Dictionary<string, string> claims);
}
