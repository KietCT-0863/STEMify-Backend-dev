using Identity.Application.Common.Interfaces;
using MediatR;

namespace Identity.Application.Auth.Commands.ProcessTokenExchange;

/// <summary>
/// Handler for ProcessTokenExchangeCommand - Clean Application Layer
/// Delegates to Infrastructure service for OAuth2 implementation
/// </summary>
public class ProcessTokenExchangeCommandHandler
    : IRequestHandler<ProcessTokenExchangeCommand, TokenExchangeResult>
{
    private readonly ITokenExchangeService _tokenExchangeService;

    public ProcessTokenExchangeCommandHandler(ITokenExchangeService tokenExchangeService)
    {
        _tokenExchangeService = tokenExchangeService;
    }

    public async Task<TokenExchangeResult> Handle(
        ProcessTokenExchangeCommand request,
        CancellationToken cancellationToken
    )
    {
        // Application layer only coordinates business logic
        // All OAuth2/OpenIddict implementation details are in Infrastructure layer
        return await _tokenExchangeService.ExchangeTokenAsync(request, cancellationToken);
    }
}
