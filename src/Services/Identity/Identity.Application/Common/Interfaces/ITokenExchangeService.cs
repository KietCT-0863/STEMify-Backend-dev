using Identity.Application.Auth.Commands.ProcessTokenExchange;

namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Interface for OAuth2 token exchange operations
/// Implementation details are in Infrastructure layer
/// </summary>
public interface ITokenExchangeService
{
    /// <summary>
    /// Exchange authorization code, refresh token, or other grant types for access tokens
    /// </summary>
    /// <param name="request">Token exchange request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token exchange result containing access tokens or error information</returns>
    Task<TokenExchangeResult> ExchangeTokenAsync(
        ProcessTokenExchangeCommand request,
        CancellationToken cancellationToken
    );
}
