using Contracts.Domains;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when an invitation is created
/// Used for audit logging and notification purposes
/// </summary>
public record InvitationCreatedEvent : DomainEvent
{
    public Guid InvitationId { get; init; }
    public int OrganizationId { get; init; }
    public string InviteeEmail { get; init; }
    public OrganizationRole TargetRole { get; init; }
    public string LicenseType { get; init; }
    public Guid? ProcessedByJobId { get; init; }

    public InvitationCreatedEvent(
        Guid invitationId,
        int organizationId,
        string inviteeEmail,
        OrganizationRole targetRole,
        string licenseType,
        Guid? processedByJobId = null)
    {
        InvitationId = invitationId;
        OrganizationId = organizationId;
        InviteeEmail = inviteeEmail;
        TargetRole = targetRole;
        LicenseType = licenseType;
        ProcessedByJobId = processedByJobId;
    }
}
