namespace Identity.Application.Common.Models.Auth;

public class TokenRequestDto
{
    public string GrantType { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Code { get; set; }
    public string? RefreshToken { get; set; }
    public string? RedirectUri { get; set; }
    public string? CodeVerifier { get; set; }
    public IEnumerable<string> Scopes { get; set; } = Array.Empty<string>();
}
