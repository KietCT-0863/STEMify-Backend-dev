using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an invitation token is invalid or malformed
/// </summary>
public class InvalidInvitationTokenException : DomainException
{
    public InvalidInvitationTokenException(string token)
        : base(
            $"The invitation token '{token}' is invalid or malformed",
            "INVALID_INVITATION_TOKEN"
        )
    {
    }

    public InvalidInvitationTokenException()
        : base(
            "The invitation token is invalid or malformed",
            "INVALID_INVITATION_TOKEN"
        )
    {
    }
}
