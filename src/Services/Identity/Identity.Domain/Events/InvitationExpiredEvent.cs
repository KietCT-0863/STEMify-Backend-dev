using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when an invitation expires
/// Used for audit logging and cleanup purposes
/// </summary>
public record InvitationExpiredEvent : DomainEvent
{
    public Guid InvitationId { get; init; }
    public int OrganizationId { get; init; }
    public string InviteeEmail { get; init; }
    public DateTime ExpiresAt { get; init; }

    public InvitationExpiredEvent(
        Guid invitationId,
        int organizationId,
        string inviteeEmail,
        DateTime expiresAt)
    {
        InvitationId = invitationId;
        OrganizationId = organizationId;
        InviteeEmail = inviteeEmail;
        ExpiresAt = expiresAt;
    }
}
