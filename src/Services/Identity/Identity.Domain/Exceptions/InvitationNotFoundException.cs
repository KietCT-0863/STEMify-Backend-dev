using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an invitation is not found
/// </summary>
public class InvitationNotFoundException : NotFoundException
{
    public InvitationNotFoundException(Guid invitationId)
        : base($"Invitation with ID {invitationId} was not found")
    {
    }

    public InvitationNotFoundException(string token)
        : base($"Invitation with token '{token}' was not found")
    {
    }
}
