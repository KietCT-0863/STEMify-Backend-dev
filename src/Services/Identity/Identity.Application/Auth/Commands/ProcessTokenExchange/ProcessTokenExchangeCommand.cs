using MediatR;

namespace Identity.Application.Auth.Commands.ProcessTokenExchange;

/// <summary>
/// Command to process OAuth token exchange
/// </summary>
public class ProcessTokenExchangeCommand : IRequest<TokenExchangeResult>
{
    public string? GrantType { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? Code { get; init; }
    public string? RedirectUri { get; init; }
    public string? CodeVerifier { get; init; }
    public string? RefreshToken { get; init; }
    public string? Scope { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
}

/// <summary>
/// Result of token exchange
/// </summary>
public class TokenExchangeResult
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? IdToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? Scope { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
}
