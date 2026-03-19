using Contracts.Domains;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user accepts an invitation
/// Triggers license assignment via Order Service integration
/// </summary>
public record InvitationAcceptedEvent : DomainEvent
{
    public Guid InvitationId { get; init; }
    public int OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; }
    public OrganizationRole TargetRole { get; init; }
    public string LicenseType { get; init; }
    public string? ClassId { get; init; }

    public InvitationAcceptedEvent(
        Guid invitationId,
        int organizationId,
        Guid userId,
        string userEmail,
        OrganizationRole targetRole,
        string licenseType,
        string? classId = null)
    {
        InvitationId = invitationId;
        OrganizationId = organizationId;
        UserId = userId;
        UserEmail = userEmail;
        TargetRole = targetRole;
        LicenseType = licenseType;
        ClassId = classId;
    }
}
