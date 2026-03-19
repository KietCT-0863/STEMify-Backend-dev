using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a bulk invitation CSV is uploaded
/// This event is subscribed by background job to process invitations asynchronously
/// </summary>
public record BulkInviteRequestedEvent : DomainEvent
{
    public Guid BulkImportJobId { get; init; }
    public int OrganizationId { get; init; }
    public int TotalCount { get; init; }
    public Guid RequestedBy { get; init; }

    public BulkInviteRequestedEvent(
        Guid bulkImportJobId,
        int organizationId,
        int totalCount,
        Guid requestedBy)
    {
        BulkImportJobId = bulkImportJobId;
        OrganizationId = organizationId;
        TotalCount = totalCount;
        RequestedBy = requestedBy;
    }
}
