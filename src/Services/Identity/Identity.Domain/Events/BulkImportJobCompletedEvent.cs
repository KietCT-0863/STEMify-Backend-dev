using Contracts.Domains;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a bulk import job completes (successfully or with failures)
/// Used for notifications and final reporting
/// </summary>
public record BulkImportJobCompletedEvent : DomainEvent
{
    public Guid JobId { get; init; }
    public int OrganizationId { get; init; }
    public BulkImportStatus Status { get; init; }
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public decimal SuccessRate { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public TimeSpan Duration { get; init; }

    public BulkImportJobCompletedEvent(
        Guid jobId,
        int organizationId,
        BulkImportStatus status,
        int totalCount,
        int successCount,
        int failedCount,
        decimal successRate,
        DateTime startedAt,
        DateTime completedAt)
    {
        JobId = jobId;
        OrganizationId = organizationId;
        Status = status;
        TotalCount = totalCount;
        SuccessCount = successCount;
        FailedCount = failedCount;
        SuccessRate = successRate;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Duration = completedAt - startedAt;
    }
}
