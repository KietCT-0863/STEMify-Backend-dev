namespace Identity.Application.Common.Interfaces.Services;

/// <summary>
/// Service for creating and validating OAuth state with encrypted invitation token
/// State is signed and time-limited to prevent CSRF and tampering attacks
/// </summary>
public interface IOAuthStateService
{
    /// <summary>
    /// Create encrypted and signed OAuth state containing invitation token and PKCE code verifier
    /// </summary>
    /// <param name="invitationToken">The invitation token to embed in state</param>
    /// <param name="codeVerifier">PKCE code verifier to embed in state</param>
    /// <returns>Encrypted and signed state string safe for URL</returns>
    string CreateState(string invitationToken, string codeVerifier);

    /// <summary>
    /// Validate and decrypt OAuth state to extract invitation token and code verifier
    /// </summary>
    /// <param name="encryptedState">The encrypted state from OAuth callback</param>
    /// <param name="invitationToken">Extracted invitation token</param>
    /// <param name="codeVerifier">Extracted PKCE code verifier</param>
    /// <returns>True if state is valid and not expired, false otherwise</returns>
    bool ValidateState(string encryptedState, out string invitationToken, out string codeVerifier);
}
