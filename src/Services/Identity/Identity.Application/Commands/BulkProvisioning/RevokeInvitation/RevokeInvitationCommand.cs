using MediatR;

namespace Identity.Application.Commands.BulkProvisioning.RevokeInvitation;

public class RevokeInvitationCommand : IRequest
{
    public Guid InvitationId { get; set; }
    public string? RevokedBy { get; set; }
    public string? Reason { get; set; }
}
