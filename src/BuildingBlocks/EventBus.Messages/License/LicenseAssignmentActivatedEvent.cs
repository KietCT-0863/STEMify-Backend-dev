using Contracts.Domains;

namespace EventBus.Messages.License;

/// <summary>
/// This event is consumed by Identity Service to sync OrganizationUser.IsActive status
/// </summary>
public record LicenseAssignmentActivatedEvent : DomainEvent
{
    public int LicenseAssignmentId { get; init; }
    public string UserId { get; init; } // Guid as string 
    public int OrganizationSubscriptionOrderId { get; init; }
    public string LicenseType { get; init; } // "Student", "Teacher", "OrganizationAdmin"
    public DateTime ActivatedAt { get; init; }

    public LicenseAssignmentActivatedEvent(
        int licenseAssignmentId,
        string userId,
        int organizationSubscriptionOrderId,
        string licenseType,
        DateTime activatedAt)
    {
        LicenseAssignmentId = licenseAssignmentId;
        UserId = userId;
        OrganizationSubscriptionOrderId = organizationSubscriptionOrderId;
        LicenseType = licenseType;
        ActivatedAt = activatedAt;
    }
}









