using Contracts.Domains;

namespace EventBus.Messages.License;

/// <summary>
/// This event is consumed by Identity Service to sync OrganizationUser license information
/// </summary>
public record LicenseAssignmentCreatedEvent : DomainEvent
{
    public int LicenseAssignmentId { get; init; }
    public string UserId { get; init; } // Guid as string 
    public int OrganizationSubscriptionOrderId { get; init; }
    public string LicenseType { get; init; } // "Student", "Teacher", "OrganizationAdmin"
    public string Status { get; init; } // "Pending", "Active"
    public DateTime AssignedAt { get; init; }

    public LicenseAssignmentCreatedEvent(
        int licenseAssignmentId,
        string userId,
        int organizationSubscriptionOrderId,
        string licenseType,
        string status,
        DateTime assignedAt)
    {
        LicenseAssignmentId = licenseAssignmentId;
        UserId = userId;
        OrganizationSubscriptionOrderId = organizationSubscriptionOrderId;
        LicenseType = licenseType;
        Status = status;
        AssignedAt = assignedAt;
    }
}









