using Contracts.Domains;

namespace EventBus.Messages.License;

public record LicenseAssignmentDeletedEvent : DomainEvent
{
    public int LicenseAssignmentId { get; init; }

    public LicenseAssignmentDeletedEvent(int licenseAssignmentId)
    {
        LicenseAssignmentId = licenseAssignmentId;
    }
}


