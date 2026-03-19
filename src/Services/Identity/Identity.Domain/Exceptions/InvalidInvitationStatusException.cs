using Identity.Domain.Enums;
using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting an operation on an invitation with invalid status
/// </summary>
public class InvalidInvitationStatusException : DomainException
{
    public InvalidInvitationStatusException(Guid invitationId, InvitationStatus currentStatus, string operation)
        : base(
            $"Cannot perform operation '{operation}' on invitation {invitationId} with status {currentStatus}",
            "INVALID_INVITATION_STATUS"
        )
    {
    }
}
