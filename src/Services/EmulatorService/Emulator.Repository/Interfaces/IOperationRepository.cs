using Emulator.Repository.Entities;

namespace Emulator.Repository.Interfaces;

/// <summary>
/// Repository for managing emulation operations (event sourcing)
/// Implements delta sync pattern for 99.75% reduction in network traffic
/// </summary>
public interface IOperationRepository
{
    /// <summary>
    /// Append a new operation to an emulation's event log
    /// Automatically generates sequence number using atomic increment
    /// </summary>
    /// <param name="operation">Operation to append (seq will be auto-generated)</param>
    /// <returns>Operation with generated seq number</returns>
    Task<EmulationOperation> AppendOperationAsync(EmulationOperation operation);

    /// <summary>
    /// Append multiple operations in a batch (atomic transaction)
    /// All operations succeed or all fail together
    /// </summary>
    /// <param name="operations">List of operations to append</param>
    /// <returns>Operations with generated seq numbers</returns>
    Task<List<EmulationOperation>> AppendBatchAsync(List<EmulationOperation> operations);

    /// <summary>
    /// Get all operations for an emulation since a sequence number
    /// Used for: syncing client state, replaying operations
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="sinceSeq">Get operations with seq > sinceSeq</param>
    /// <param name="includeDeleted">Whether to include soft-deleted operations</param>
    /// <returns>List of operations ordered by seq ASC</returns>
    Task<List<EmulationOperation>> GetOperationsSinceAsync(
        string emulationId,
        long sinceSeq,
        bool includeDeleted = false);

    /// <summary>
    /// Get operations in a sequence range (inclusive)
    /// Used for: snapshot generation, operation replay
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="fromSeq">Start sequence (inclusive)</param>
    /// <param name="toSeq">End sequence (inclusive)</param>
    /// <param name="includeDeleted">Whether to include soft-deleted operations</param>
    /// <returns>List of operations ordered by seq ASC</returns>
    Task<List<EmulationOperation>> GetOperationsRangeAsync(
        string emulationId,
        long fromSeq,
        long toSeq,
        bool includeDeleted = false);

    /// <summary>
    /// Get operations by batch ID
    /// Used for: undoing a batch of operations together
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="batchId">Batch ID from metadata</param>
    /// <returns>List of operations in the batch</returns>
    Task<List<EmulationOperation>> GetOperationsByBatchAsync(string emulationId, string batchId);

    /// <summary>
    /// Get the latest sequence number for an emulation
    /// Used for: conflict detection, sync status
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <returns>Latest seq number, or 0 if no operations</returns>
    Task<long> GetLatestSeqAsync(string emulationId);

    /// <summary>
    /// Mark operations as applied to snapshot
    /// Used after snapshot generation to track which operations are safe to prune
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="snapshotSeq">Snapshot sequence number</param>
    /// <param name="operationSeqs">List of operation seq numbers to mark</param>
    /// <returns>Number of operations marked</returns>
    Task<int> MarkOperationsAsAppliedAsync(
        string emulationId,
        long snapshotSeq,
        List<long> operationSeqs);

    /// <summary>
    /// Soft delete old operations (prune)
    /// Per architecture: Keep last 1000 operations
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="beforeSeq">Delete operations with seq < beforeSeq</param>
    /// <param name="onlyApplied">Only delete operations already applied to snapshot</param>
    /// <returns>Number of operations soft-deleted</returns>
    Task<int> PruneOperationsAsync(
        string emulationId,
        long beforeSeq,
        bool onlyApplied = true);

    /// <summary>
    /// Hard delete operations (permanent removal)
    /// Use with caution - this is irreversible
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="beforeSeq">Delete operations with seq < beforeSeq</param>
    /// <returns>Number of operations permanently deleted</returns>
    Task<int> HardDeleteOperationsAsync(string emulationId, long beforeSeq);

    /// <summary>
    /// Get operation statistics for monitoring
    /// Used for: /metrics/operations endpoint, dashboard
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <returns>Operation statistics</returns>
    Task<OperationStatistics> GetStatisticsAsync(string emulationId);

    /// <summary>
    /// Count operations for an emulation
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="includeDeleted">Whether to include soft-deleted operations</param>
    /// <returns>Total count</returns>
    Task<long> CountOperationsAsync(string emulationId, bool includeDeleted = false);

    /// <summary>
    /// Check if emulation has any operations
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <returns>True if operations exist</returns>
    Task<bool> HasOperationsAsync(string emulationId);
}

/// <summary>
/// Operation statistics for monitoring and metrics
/// </summary>
public class OperationStatistics
{
    /// <summary>
    /// Total operations (including soft-deleted)
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Operations not yet applied to snapshot
    /// </summary>
    public long PendingOperations { get; set; }

    /// <summary>
    /// Operations already applied to snapshot
    /// </summary>
    public long AppliedOperations { get; set; }

    /// <summary>
    /// Soft-deleted operations
    /// </summary>
    public long DeletedOperations { get; set; }

    /// <summary>
    /// Latest sequence number
    /// </summary>
    public long LastSeq { get; set; }

    /// <summary>
    /// Last snapshot sequence number
    /// </summary>
    public long LastSnapshotSeq { get; set; }

    /// <summary>
    /// Operations since last snapshot (needs replay)
    /// </summary>
    public long OperationsSinceSnapshot => LastSeq - LastSnapshotSeq;

    /// <summary>
    /// When last operation was created
    /// </summary>
    public DateTime? LastOperationAt { get; set; }

    /// <summary>
    /// When last snapshot was created
    /// </summary>
    public DateTime? LastSnapshotAt { get; set; }

    /// <summary>
    /// Operations per operation type
    /// </summary>
    public Dictionary<string, long> OperationsByType { get; set; } = new();

    /// <summary>
    /// Operations per user
    /// </summary>
    public Dictionary<string, long> OperationsByUser { get; set; } = new();

    /// <summary>
    /// Average operations per day (last 7 days)
    /// </summary>
    public double AverageOperationsPerDay { get; set; }

    /// <summary>
    /// Get summary string for logging
    /// </summary>
    public string GetSummary()
    {
        return $"Total: {TotalOperations}, Pending: {PendingOperations}, Applied: {AppliedOperations}, " +
               $"Deleted: {DeletedOperations}, LastSeq: {LastSeq}, SinceSnapshot: {OperationsSinceSnapshot}";
    }
}
