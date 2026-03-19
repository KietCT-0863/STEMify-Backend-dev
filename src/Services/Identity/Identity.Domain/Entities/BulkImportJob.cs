using Identity.Domain.Common;
using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Tracks progress and status of CSV-based user provisioning
/// </summary>
public class BulkImportJob : BaseEntity<Guid>
{
    public int OrganizationId { get; private set; }     
    public BulkImportStatus Status { get; private set; }
    public int? SubscriptionOrderId { get; private set; }

    // Progress tracking
    public int TotalCount { get; private set; }
    public int ProcessedCount { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailedCount { get; private set; }

    // Calculated property for progress percentage
    public decimal ProgressPercentage => TotalCount > 0
        ? Math.Round((decimal)ProcessedCount / TotalCount * 100, 2)
        : 0;

    // CSV data storage (serialized as JSON for worker to process)
    public string CsvDataJson { get; private set; } = string.Empty;

    // Metadata
    public Guid CreatedBy { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<BulkImportFailure> _failures = new();
    public IReadOnlyCollection<BulkImportFailure> Failures => _failures.AsReadOnly();

    // Performance metrics
    public TimeSpan? ProcessingDuration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    private BulkImportJob() { }

    /// <summary>
    /// Factory method to create a new bulk import job
    /// </summary>
    public static BulkImportJob Create(
        int organizationId,
        string csvDataJson,
        int totalCount,
        Guid createdBy,
        int? subscriptionOrderId = null)
    {
        if (totalCount <= 0)
            throw new ArgumentException("Total count must be greater than zero", nameof(totalCount));

        if (string.IsNullOrWhiteSpace(csvDataJson))
            throw new ArgumentException("CSV data cannot be empty", nameof(csvDataJson));

        var job = new BulkImportJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SubscriptionOrderId = subscriptionOrderId,
            TotalCount = totalCount,
            ProcessedCount = 0,
            SuccessCount = 0,
            FailedCount = 0,
            Status = BulkImportStatus.Pending,
            CsvDataJson = csvDataJson,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        job.AddDomainEvent(new BulkImportJobCreatedEvent(
            jobId: job.Id,
            organizationId: organizationId,
            totalCount: totalCount,
            createdBy: createdBy
        ));

        // Publish BulkInviteRequestedEvent to trigger background worker
        job.AddDomainEvent(new BulkInviteRequestedEvent(
            bulkImportJobId: job.Id,
            organizationId: organizationId,
            totalCount: totalCount,
            requestedBy: createdBy
        ));

        return job;
    }

    /// <summary>
    /// Start processing the job
    /// Business rule: Can only start if status is Pending
    /// </summary>
    public void Start()
    {
        if (Status != BulkImportStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot start job with status: {Status}. Job must be in Pending status."
            );

        Status = BulkImportStatus.Processing;
        StartedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new BulkImportJobStartedEvent(
            jobId: Id,
            startedAt: StartedAt.Value
        ));
    }

    /// <summary>
    /// Record a successful invitation processing
    /// </summary>
    public void RecordSuccess()
    {
        if (Status != BulkImportStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot record success for job with status: {Status}"
            );

        ProcessedCount++;
        SuccessCount++;
        UpdatedAt = DateTime.UtcNow;

        CheckCompletion();

        // Publish progress event every 10% or every 50 items
        if (ShouldPublishProgressEvent())
        {
            AddDomainEvent(new BulkImportJobProgressUpdatedEvent(
                jobId: Id,
                totalCount: TotalCount,
                processedCount: ProcessedCount,
                successCount: SuccessCount,
                failedCount: FailedCount,
                progressPercentage: ProgressPercentage,
                estimatedTimeRemaining: GetEstimatedTimeRemaining()
            ));
        }
    }

    /// <summary>
    /// Record a failed invitation processing with reason
    /// </summary>
    public void RecordFailure(string email, string reason)
    {
        if (Status != BulkImportStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot record failure for job with status: {Status}"
            );

        ProcessedCount++;
        FailedCount++;

        _failures.Add(new BulkImportFailure
        {
            Email = email,
            Reason = reason,
            FailedAt = DateTime.UtcNow
        });

        UpdatedAt = DateTime.UtcNow;

        CheckCompletion();

        // Publish progress event
        if (ShouldPublishProgressEvent())
        {
            AddDomainEvent(new BulkImportJobProgressUpdatedEvent(
                jobId: Id,
                totalCount: TotalCount,
                processedCount: ProcessedCount,
                successCount: SuccessCount,
                failedCount: FailedCount,
                progressPercentage: ProgressPercentage,
                estimatedTimeRemaining: GetEstimatedTimeRemaining()
            ));
        }
    }

    /// <summary>
    /// Mark job as failed (system error, not individual invitation failures)
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = BulkImportStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Record system failure
        _failures.Add(new BulkImportFailure
        {
            Email = "SYSTEM",
            Reason = reason,
            FailedAt = DateTime.UtcNow
        });

        AddDomainEvent(new BulkImportJobFailedEvent(
            jobId: Id,
            organizationId: OrganizationId,
            failureReason: reason,
            failedAt: CompletedAt.Value,
            processedCount: ProcessedCount,
            totalCount: TotalCount
        ));
    }

    /// <summary>
    /// Get success rate as percentage
    /// </summary>
    public decimal GetSuccessRate()
    {
        if (TotalCount == 0) return 0;
        return Math.Round((decimal)SuccessCount / TotalCount * 100, 2);
    }

    /// <summary>
    /// Get failure rate as percentage
    /// </summary>
    public decimal GetFailureRate()
    {
        if (TotalCount == 0) return 0;
        return Math.Round((decimal)FailedCount / TotalCount * 100, 2);
    }

    /// <summary>
    /// Check if job is complete and update status
    /// Business rule: Job is complete when all items are processed
    /// </summary>
    private void CheckCompletion()
    {
        if (ProcessedCount >= TotalCount)
        {
            Status = BulkImportStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BulkImportJobCompletedEvent(
                jobId: Id,
                organizationId: OrganizationId,
                status: Status,
                totalCount: TotalCount,
                successCount: SuccessCount,
                failedCount: FailedCount,
                successRate: GetSuccessRate(),
                startedAt: StartedAt!.Value,
                completedAt: CompletedAt.Value
            ));
        }
    }

    /// <summary>
    /// Determine if progress event should be published
    /// Publish every 10% progress or every 50 items
    /// </summary>
    private bool ShouldPublishProgressEvent()
    {
        // Publish at every 10% milestone
        var progressMilestone = (int)(ProgressPercentage / 10) * 10;
        var previousProgress = ProcessedCount > 1
            ? Math.Round((decimal)(ProcessedCount - 1) / TotalCount * 100, 2)
            : 0;
        var previousMilestone = (int)(previousProgress / 10) * 10;

        // Publish if crossed 10% milestone or every 50 items
        return progressMilestone > previousMilestone || ProcessedCount % 50 == 0;
    }

    /// <summary>
    /// Check if job is still in progress
    /// </summary>
    public bool IsInProgress()
    {
        return Status == BulkImportStatus.Processing;
    }

    /// <summary>
    /// Check if job is completed (successfully or with partial failures)
    /// </summary>
    public bool IsCompleted()
    {
        return Status == BulkImportStatus.Completed;
    }

    /// <summary>
    /// Check if job has failed completely
    /// </summary>
    public bool HasFailed()
    {
        return Status == BulkImportStatus.Failed;
    }

    /// <summary>
    /// Get estimated time remaining based on current processing rate
    /// </summary>
    public TimeSpan? GetEstimatedTimeRemaining()
    {
        if (!StartedAt.HasValue || ProcessedCount == 0)
            return null;

        var elapsed = DateTime.UtcNow - StartedAt.Value;
        var avgTimePerItem = elapsed.TotalSeconds / ProcessedCount;
        var remainingItems = TotalCount - ProcessedCount;
        var estimatedSeconds = avgTimePerItem * remainingItems;

        return TimeSpan.FromSeconds(estimatedSeconds);
    }

    /// <summary>
    /// Get average processing rate (items per second)
    /// </summary>
    public double? GetProcessingRate()
    {
        if (!StartedAt.HasValue || ProcessedCount == 0)
            return null;

        var elapsed = DateTime.UtcNow - StartedAt.Value;
        return ProcessedCount / elapsed.TotalSeconds;
    }
}

/// <summary>
/// Value object representing a bulk import failure record
/// Owned entity by BulkImportJob
/// </summary>
public class BulkImportFailure
{
    public string Email { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
