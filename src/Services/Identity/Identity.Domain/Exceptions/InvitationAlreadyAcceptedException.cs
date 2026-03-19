using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to accept an invitation that has already been accepted
/// </summary>
public class InvitationAlreadyAcceptedException : DomainException
{
    public InvitationAlreadyAcceptedException(Guid invitationId, Guid acceptedUserId)
        : base(
            $"Invitation {invitationId} has already been accepted by user {acceptedUserId}",
            "INVITATION_ALREADY_ACCEPTED"
        )
    {
    }
}
