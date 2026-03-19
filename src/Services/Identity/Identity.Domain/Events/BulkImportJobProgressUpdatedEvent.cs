using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when bulk import job progress is updated
/// Published every 10% progress or every 50 items for real-time progress tracking
/// </summary>
public record BulkImportJobProgressUpdatedEvent : DomainEvent
{
    public Guid JobId { get; init; }
    public int TotalCount { get; init; }
    public int ProcessedCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public decimal ProgressPercentage { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    public BulkImportJobProgressUpdatedEvent(
        Guid jobId,
        int totalCount,
        int processedCount,
        int successCount,
        int failedCount,
        decimal progressPercentage,
        TimeSpan? estimatedTimeRemaining = null)
    {
        JobId = jobId;
        TotalCount = totalCount;
        ProcessedCount = processedCount;
        SuccessCount = successCount;
        FailedCount = failedCount;
        ProgressPercentage = progressPercentage;
        EstimatedTimeRemaining = estimatedTimeRemaining;
    }
}
