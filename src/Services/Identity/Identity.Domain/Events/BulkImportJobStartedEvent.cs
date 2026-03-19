using Contracts.Domains;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a bulk import job starts processing
/// Used for real-time status updates to clients
/// </summary>
public record BulkImportJobStartedEvent : DomainEvent
{
    public Guid JobId { get; init; }
    public DateTime StartedAt { get; init; }

    public BulkImportJobStartedEvent(Guid jobId, DateTime startedAt)
    {
        JobId = jobId;
        StartedAt = startedAt;
    }
}
