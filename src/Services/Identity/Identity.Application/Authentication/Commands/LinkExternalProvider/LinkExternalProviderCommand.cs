using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Authentication.Commands.LinkExternalProvider;

/// <summary>
/// Command to link an external authentication provider to an existing user account
/// </summary>
public record LinkExternalProviderCommand : IRequest<bool>
{
    public Guid UserId { get; init; }

    public ExternalLoginInfoDto ExternalLoginInfo { get; init; } = null!;
}
