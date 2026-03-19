using Identity.Application.Auth.Commands.ProcessAuthorization;

namespace Identity.Application.Common.Interfaces;

public interface IAuthorizationProcessingService
{
    Task<AuthorizationResult> ProcessAuthorizationAsync(ProcessAuthorizationCommand command);
}
