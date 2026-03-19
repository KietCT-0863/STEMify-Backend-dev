using Contracts.Domains;

namespace EventBus.Messages.License;

/// <summary>
/// This event is consumed by Identity Service to sync OrganizationUser.IsActive = false
/// </summary>
public record LicenseAssignmentRevokedEvent : DomainEvent
{
    public int LicenseAssignmentId { get; init; }
    public string UserId { get; init; } // Guid as string 
    public int OrganizationSubscriptionOrderId { get; init; }
    public string LicenseType { get; init; } // "Student", "Teacher", "OrganizationAdmin"
    public DateTime RevokedAt { get; init; }
    public string? Reason { get; init; }

    public LicenseAssignmentRevokedEvent(
        int licenseAssignmentId,
        string userId,
        int organizationSubscriptionOrderId,
        string licenseType,
        DateTime revokedAt,
        string? reason = null)
    {
        LicenseAssignmentId = licenseAssignmentId;
        UserId = userId;
        OrganizationSubscriptionOrderId = organizationSubscriptionOrderId;
        LicenseType = licenseType;
        RevokedAt = revokedAt;
        Reason = reason;
    }
}









