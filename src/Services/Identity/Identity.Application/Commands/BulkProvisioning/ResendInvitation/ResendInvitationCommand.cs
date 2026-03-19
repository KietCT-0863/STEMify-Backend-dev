using MediatR;

namespace Identity.Application.Commands.BulkProvisioning.ResendInvitation;

public class ResendInvitationCommand : IRequest
{
    public Guid InvitationId { get; set; }
}
