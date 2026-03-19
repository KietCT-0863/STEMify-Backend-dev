using MediatR;

namespace Identity.Application.Authentication.Commands.UnlinkExternalProvider;

/// <summary>
/// Command to unlink an external authentication provider from a user account
/// </summary>
public record UnlinkExternalProviderCommand : IRequest<bool>
{
    public Guid UserId { get; init; }

    public string ProviderName { get; init; } = string.Empty;
}
