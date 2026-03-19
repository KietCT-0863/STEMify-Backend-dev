using Identity.Application.Dtos.Users;
using MediatR;

namespace Identity.Application.Commands.BulkProvisioning.AcceptInvitation;

/// <summary>
/// Command to accept invitation after Google SSO authentication
/// </summary>
public class AcceptInvitationCommand : IRequest<UserDto>
{
    public string InvitationToken { get; set; } = null!;
    public string GoogleEmail { get; set; } = null!;
    public string? GoogleId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
