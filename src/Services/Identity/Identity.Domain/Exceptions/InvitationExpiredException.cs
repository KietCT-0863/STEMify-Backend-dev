using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to accept an expired invitation
/// </summary>
public class InvitationExpiredException : DomainException
{
    public InvitationExpiredException(Guid invitationId, DateTime expiresAt)
        : base(
            $"Invitation {invitationId} has expired at {expiresAt:yyyy-MM-dd HH:mm:ss} UTC",
            "INVITATION_EXPIRED"
        )
    {
    }
}
