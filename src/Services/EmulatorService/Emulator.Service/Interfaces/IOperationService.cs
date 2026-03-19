using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Shared.SeedWork;

namespace Emulator.Service.Interfaces;

/// <summary>
/// Provides business logic, validation, and conflict resolution
/// </summary>
public interface IOperationService
{
    /// <summary>
    /// Append a single operation to an emulation
    /// Validates operation before appending
    /// </summary>
    /// <param name="request">Operation append request</param>
    /// <returns>Result with appended operation (including generated seq)</returns>
    Task<ApiResult<EmulationOperation>> AppendOperationAsync(AppendOperationRequest request);

    /// <summary>
    /// Append multiple operations as a batch (atomic)
    /// All operations succeed or all fail together
    /// </summary>
    /// <param name="request">Batch operation request</param>
    /// <returns>Result with appended operations (including generated seqs)</returns>
    Task<ApiResult<List<EmulationOperation>>> AppendBatchOperationsAsync(AppendBatchRequest request);

    /// <summary>
    /// Get operations since a sequence number
    /// Used for syncing client state
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="sinceSeq">Get operations with seq > sinceSeq</param>
    /// <returns>List of operations</returns>
    Task<ApiResult<List<EmulationOperation>>> GetOperationsSinceAsync(string emulationId, long sinceSeq);

    /// <summary>
    /// Get operation history for an emulation
    /// Supports pagination for large histories
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="fromSeq">Start sequence (optional, default 0)</param>
    /// <param name="limit">Max operations to return (optional, default 100)</param>
    /// <returns>List of operations</returns>
    Task<ApiResult<List<EmulationOperation>>> GetOperationHistoryAsync(
        string emulationId,
        long? fromSeq = null,
        int limit = 100);

    /// <summary>
    /// Get operations by batch ID
    /// Used for batch undo/redo
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="batchId">Batch ID</param>
    /// <returns>List of operations in the batch</returns>
    Task<ApiResult<List<EmulationOperation>>> GetOperationsByBatchAsync(string emulationId, string batchId);

    /// <summary>
    /// Get operation statistics for monitoring
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <returns>Operation statistics</returns>
    Task<ApiResult<OperationStatistics>> GetOperationStatisticsAsync(string emulationId);

    /// <summary>
    /// Validate an operation before applying
    /// Checks: valid op type, valid path, valid value type
    /// </summary>
    /// <param name="operation">Operation to validate</param>
    /// <returns>Validation result with errors if any</returns>
    Task<OperationValidationResult> ValidateOperationAsync(EmulationOperation operation);

    /// <summary>
    /// Detect conflicts between operations
    /// Used for concurrent editing scenarios
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <param name="operation">New operation to check</param>
    /// <param name="clientLastSeq">Client's last known sequence</param>
    /// <returns>Conflict detection result</returns>
    Task<ConflictDetectionResult> DetectConflictsAsync(
        string emulationId,
        EmulationOperation operation,
        long clientLastSeq);

    /// <summary>
    /// Get current sequence number for an emulation
    /// Used for sync status checks
    /// </summary>
    /// <param name="emulationId">Emulation ID</param>
    /// <returns>Current sequence number</returns>
    Task<ApiResult<long>> GetCurrentSequenceAsync(string emulationId);
}

/// <summary>
/// Request to append a single operation
/// </summary>
public class AppendOperationRequest
{
    public string EmulationId { get; set; } = string.Empty;
    public string Op { get; set; } = string.Empty;  // add, remove, replace, move, copy, test
    public string Path { get; set; } = string.Empty;
    public object? Value { get; set; }
    public object? OldValue { get; set; }
    public string? From { get; set; }
    public string UserId { get; set; } = string.Empty;
    public OperationMetadata? Metadata { get; set; }
    public long? ClientLastSeq { get; set; }  // For conflict detection
}

/// <summary>
/// Request to append multiple operations as batch
/// </summary>
public class AppendBatchRequest
{
    public string EmulationId { get; set; } = string.Empty;
    public List<OperationDto> Operations { get; set; } = new();
    public string UserId { get; set; } = string.Empty;
    public string? BatchId { get; set; }
    public long? ClientLastSeq { get; set; }  // For conflict detection
}

/// <summary>
/// Single operation DTO (without emulationId, userId - from batch)
/// </summary>
public class OperationDto
{
    public string Op { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public object? Value { get; set; }
    public object? OldValue { get; set; }
    public string? From { get; set; }
    public OperationMetadata? Metadata { get; set; }
}

/// <summary>
/// Operation validation result
/// </summary>
public class OperationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static OperationValidationResult Success()
    {
        return new OperationValidationResult { IsValid = true };
    }

    public static OperationValidationResult Failure(string error)
    {
        return new OperationValidationResult
        {
            IsValid = false,
            Errors = new List<string> { error }
        };
    }

    public static OperationValidationResult Failure(List<string> errors)
    {
        return new OperationValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Conflict detection result
/// </summary>
public class ConflictDetectionResult
{
    public bool HasConflict { get; set; }
    public ConflictType ConflictType { get; set; }
    public List<EmulationOperation> ConflictingOperations { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public ConflictResolutionStrategy SuggestedResolution { get; set; }
}

/// <summary>
/// Types of conflicts
/// </summary>
public enum ConflictType
{
    None = 0,
    SamePathModified = 1,      // Same path modified by different users
    DependentPathModified = 2,  // Parent/child path modified
    ClientBehind = 3,           // Client is behind (needs sync first)
    StaleOperation = 4          // Operation is too old
}

/// <summary>
/// Conflict resolution strategies
/// </summary>
public enum ConflictResolutionStrategy
{
    Accept = 0,           // Accept the operation (no real conflict)
    LastWriteWins = 1,    // Use last-write-wins (default)
    RejectAndSync = 2,    // Reject operation, client must sync first
    ManualResolve = 3     // Requires manual resolution
}
