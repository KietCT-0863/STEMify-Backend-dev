using Identity.Application.Common.Interfaces;
using MediatR;

namespace Identity.Application.Auth.Commands.ProcessAuthorization;

/// <summary>
/// Handler for ProcessAuthorizationCommand
/// </summary>
public class ProcessAuthorizationCommandHandler
    : IRequestHandler<ProcessAuthorizationCommand, AuthorizationResult>
{
    private readonly IAuthorizationProcessingService _authorizationService;

    public ProcessAuthorizationCommandHandler(IAuthorizationProcessingService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<AuthorizationResult> Handle(
        ProcessAuthorizationCommand request,
        CancellationToken cancellationToken
    )
    {
        return await _authorizationService.ProcessAuthorizationAsync(request);
    }
}
