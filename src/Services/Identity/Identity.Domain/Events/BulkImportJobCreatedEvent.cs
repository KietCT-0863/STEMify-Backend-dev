using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a bulk import job is created
/// Used for audit logging
/// </summary>
public record BulkImportJobCreatedEvent : DomainEvent
{
    public Guid JobId { get; init; }
    public int OrganizationId { get; init; }
    public int TotalCount { get; init; }
    public Guid CreatedBy { get; init; }

    public BulkImportJobCreatedEvent(
        Guid jobId,
        int organizationId,
        int totalCount,
        Guid createdBy)
    {
        JobId = jobId;
        OrganizationId = organizationId;
        TotalCount = totalCount;
        CreatedBy = createdBy;
    }
}
