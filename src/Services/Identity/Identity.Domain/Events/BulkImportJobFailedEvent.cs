using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a bulk import job fails due to system error
/// Different from individual invitation failures - this is for complete job failure
/// Used for alerting, monitoring, and admin notifications
/// </summary>
public record BulkImportJobFailedEvent : DomainEvent
{
    public Guid JobId { get; init; }
    public int OrganizationId { get; init; }
    public string FailureReason { get; init; }
    public DateTime FailedAt { get; init; }
    public int ProcessedCount { get; init; }
    public int TotalCount { get; init; }

    public BulkImportJobFailedEvent(
        Guid jobId,
        int organizationId,
        string failureReason,
        DateTime failedAt,
        int processedCount,
        int totalCount)
    {
        JobId = jobId;
        OrganizationId = organizationId;
        FailureReason = failureReason;
        FailedAt = failedAt;
        ProcessedCount = processedCount;
        TotalCount = totalCount;
    }
}
