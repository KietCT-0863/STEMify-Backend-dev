using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Emulator.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.SeedWork;
using System.Text.RegularExpressions;

namespace Emulator.Service.Services;

/// <summary>
/// Provides business logic, validation, and conflict resolution for delta sync
/// </summary>
public class OperationService : IOperationService
{
    private readonly IOperationRepository _operationRepository;
    private readonly IEmulationRepository _emulationRepository;
    private readonly ILogger<OperationService> _logger;

    // Valid JSON Patch operation types (RFC 6902)
    private static readonly HashSet<string> ValidOperationTypes = new()
    {
        "add", "remove", "replace", "move", "copy", "test"
    };

    // JSON Pointer path regex (RFC 6901)
    private static readonly Regex JsonPointerRegex = new(@"^(/[^/~]*(~[01][^/~]*)*)*$", RegexOptions.Compiled);

    public OperationService(
        IOperationRepository operationRepository,
        IEmulationRepository emulationRepository,
        ILogger<OperationService> logger)
    {
        _operationRepository = operationRepository;
        _emulationRepository = emulationRepository;
        _logger = logger;
    }

    public async Task<ApiResult<EmulationOperation>> AppendOperationAsync(AppendOperationRequest request)
    {
        _logger.LogInformation("Appending operation to emulation: {EmulationId}, op: {Op}, path: {Path}",
            request.EmulationId, request.Op, request.Path);

        try
        {
            // 1. Verify emulation exists
            var emulationExists = await _emulationRepository.ExistsAsync(request.EmulationId);
            if (!emulationExists)
            {
                return ApiResult<EmulationOperation>.Failed("Emulation not found", 404);
            }

            // 2. Create operation entity
            var operation = new EmulationOperation
            {
                EmulationId = request.EmulationId,
                Op = request.Op.ToLower(),
                Path = request.Path,
                Value = request.Value,
                OldValue = request.OldValue,
                From = request.From,
                UserId = request.UserId,
                Metadata = request.Metadata
            };

            // 3. Validate operation
            var validationResult = await ValidateOperationAsync(operation);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                _logger.LogWarning("Operation validation failed: {Errors}", errors);
                return ApiResult<EmulationOperation>.Failed($"Operation validation failed: {errors}", 400);
            }

            // 4. Detect conflicts (if client sequence provided)
            if (request.ClientLastSeq.HasValue)
            {
                var conflictResult = await DetectConflictsAsync(
                    request.EmulationId,
                    operation,
                    request.ClientLastSeq.Value);

                if (conflictResult.HasConflict &&
                    conflictResult.SuggestedResolution == ConflictResolutionStrategy.RejectAndSync)
                {
                    _logger.LogWarning("Conflict detected: {Message}", conflictResult.Message);
                    return ApiResult<EmulationOperation>.Failed(
                        $"Conflict detected: {conflictResult.Message}. Please sync and retry.", 409);
                }

                if (conflictResult.HasConflict)
                {
                    _logger.LogInformation("Conflict detected but using {Strategy}: {Message}",
                        conflictResult.SuggestedResolution, conflictResult.Message);
                }
            }

            // 5. Append operation to repository (atomic seq generation)
            var appendedOperation = await _operationRepository.AppendOperationAsync(operation);

            _logger.LogInformation("Operation appended successfully: {EmulationId}, seq: {Seq}",
                request.EmulationId, appendedOperation.Seq);

            return ApiResult<EmulationOperation>.Succeeded(
                appendedOperation,
                $"Operation appended with seq: {appendedOperation.Seq}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append operation to emulation: {EmulationId}", request.EmulationId);
            return ApiResult<EmulationOperation>.Failed($"Failed to append operation: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<EmulationOperation>>> AppendBatchOperationsAsync(AppendBatchRequest request)
    {
        _logger.LogInformation("Appending batch of {Count} operations to emulation: {EmulationId}",
            request.Operations.Count, request.EmulationId);

        try
        {
            // 1. Verify emulation exists
            var emulationExists = await _emulationRepository.ExistsAsync(request.EmulationId);
            if (!emulationExists)
            {
                return ApiResult<List<EmulationOperation>>.Failed("Emulation not found", 404);
            }

            // 2. Generate batch ID if not provided
            var batchId = request.BatchId ?? $"batch_{Guid.NewGuid():N}";

            // 3. Convert DTOs to entities
            var operations = request.Operations.Select(dto => new EmulationOperation
            {
                EmulationId = request.EmulationId,
                Op = dto.Op.ToLower(),
                Path = dto.Path,
                Value = dto.Value,
                OldValue = dto.OldValue,
                From = dto.From,
                UserId = request.UserId,
                Metadata = dto.Metadata ?? new OperationMetadata()
            }).ToList();

            // Set batch ID for all operations
            foreach (var op in operations)
            {
                if (op.Metadata == null)
                    op.Metadata = new OperationMetadata();
                op.Metadata.BatchId = batchId;
            }

            // 4. Validate all operations
            var validationErrors = new List<string>();
            for (int i = 0; i < operations.Count; i++)
            {
                var validationResult = await ValidateOperationAsync(operations[i]);
                if (!validationResult.IsValid)
                {
                    validationErrors.Add($"Operation {i}: {string.Join(", ", validationResult.Errors)}");
                }
            }

            if (validationErrors.Any())
            {
                var errors = string.Join("; ", validationErrors);
                _logger.LogWarning("Batch validation failed: {Errors}", errors);
                return ApiResult<List<EmulationOperation>>.Failed($"Batch validation failed: {errors}", 400);
            }

            // 5. Detect conflicts (if client sequence provided)
            if (request.ClientLastSeq.HasValue)
            {
                // For batch, just check if client is behind
                var currentSeq = await _operationRepository.GetLatestSeqAsync(request.EmulationId);
                if (request.ClientLastSeq.Value < currentSeq)
                {
                    _logger.LogWarning("Client is behind: clientSeq={ClientSeq}, currentSeq={CurrentSeq}",
                        request.ClientLastSeq.Value, currentSeq);

                    // Still allow if difference is small (< 100 operations)
                    if (currentSeq - request.ClientLastSeq.Value > 100)
                    {
                        return ApiResult<List<EmulationOperation>>.Failed(
                            $"Client is too far behind (missed {currentSeq - request.ClientLastSeq.Value} operations). Please sync first.", 409);
                    }
                }
            }

            // 6. Append batch atomically
            var appendedOperations = await _operationRepository.AppendBatchAsync(operations);

            _logger.LogInformation("Batch appended successfully: {EmulationId}, batchId: {BatchId}, seqs: {StartSeq}-{EndSeq}",
                request.EmulationId, batchId, appendedOperations.First().Seq, appendedOperations.Last().Seq);

            return ApiResult<List<EmulationOperation>>.Succeeded(
                appendedOperations,
                $"Batch appended: {appendedOperations.Count} operations with seqs {appendedOperations.First().Seq}-{appendedOperations.Last().Seq}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append batch to emulation: {EmulationId}", request.EmulationId);
            return ApiResult<List<EmulationOperation>>.Failed($"Failed to append batch: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<EmulationOperation>>> GetOperationsSinceAsync(string emulationId, long sinceSeq)
    {
        _logger.LogDebug("Getting operations since seq {Seq} for emulation: {EmulationId}", sinceSeq, emulationId);

        try
        {
            var operations = await _operationRepository.GetOperationsSinceAsync(emulationId, sinceSeq);
            return ApiResult<List<EmulationOperation>>.Succeeded(operations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operations since seq {Seq}", sinceSeq);
            return ApiResult<List<EmulationOperation>>.Failed($"Failed to get operations: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<EmulationOperation>>> GetOperationHistoryAsync(
        string emulationId,
        long? fromSeq = null,
        int limit = 100)
    {
        _logger.LogDebug("Getting operation history for emulation: {EmulationId}, fromSeq: {FromSeq}, limit: {Limit}",
            emulationId, fromSeq, limit);

        try
        {
            // Validate limit
            if (limit <= 0 || limit > 1000)
            {
                return ApiResult<List<EmulationOperation>>.Failed("Limit must be between 1 and 1000", 400);
            }

            List<EmulationOperation> operations;

            if (fromSeq.HasValue)
            {
                // Get range from fromSeq to fromSeq + limit
                var currentSeq = await _operationRepository.GetLatestSeqAsync(emulationId);
                var toSeq = Math.Min(fromSeq.Value + limit - 1, currentSeq);
                operations = await _operationRepository.GetOperationsRangeAsync(emulationId, fromSeq.Value, toSeq);
            }
            else
            {
                // Get latest N operations
                operations = await _operationRepository.GetOperationsSinceAsync(emulationId, 0);
                operations = operations.OrderByDescending(x => x.Seq).Take(limit).OrderBy(x => x.Seq).ToList();
            }

            return ApiResult<List<EmulationOperation>>.Succeeded(operations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation history for emulation: {EmulationId}", emulationId);
            return ApiResult<List<EmulationOperation>>.Failed($"Failed to get operation history: {ex.Message}");
        }
    }

    public async Task<ApiResult<List<EmulationOperation>>> GetOperationsByBatchAsync(string emulationId, string batchId)
    {
        _logger.LogDebug("Getting operations for batch: {BatchId} in emulation: {EmulationId}", batchId, emulationId);

        try
        {
            var operations = await _operationRepository.GetOperationsByBatchAsync(emulationId, batchId);
            return ApiResult<List<EmulationOperation>>.Succeeded(operations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operations for batch: {BatchId}", batchId);
            return ApiResult<List<EmulationOperation>>.Failed($"Failed to get batch operations: {ex.Message}");
        }
    }

    public async Task<ApiResult<OperationStatistics>> GetOperationStatisticsAsync(string emulationId)
    {
        _logger.LogDebug("Getting operation statistics for emulation: {EmulationId}", emulationId);

        try
        {
            var statistics = await _operationRepository.GetStatisticsAsync(emulationId);
            return ApiResult<OperationStatistics>.Succeeded(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation statistics for emulation: {EmulationId}", emulationId);
            return ApiResult<OperationStatistics>.Failed($"Failed to get statistics: {ex.Message}");
        }
    }

    public Task<OperationValidationResult> ValidateOperationAsync(EmulationOperation operation)
    {
        var errors = new List<string>();

        // 1. Validate operation type
        if (string.IsNullOrWhiteSpace(operation.Op))
        {
            errors.Add("Operation type is required");
        }
        else if (!ValidOperationTypes.Contains(operation.Op.ToLower()))
        {
            errors.Add($"Invalid operation type: {operation.Op}. Must be one of: {string.Join(", ", ValidOperationTypes)}");
        }

        // 2. Validate path (JSON Pointer format)
        if (string.IsNullOrWhiteSpace(operation.Path))
        {
            errors.Add("Path is required");
        }
        else if (!IsValidJsonPointer(operation.Path))
        {
            errors.Add($"Invalid JSON Pointer path: {operation.Path}. Must start with '/' and follow RFC 6901");
        }

        // 3. Validate operation-specific requirements
        var opType = operation.Op?.ToLower();
        switch (opType)
        {
            case "add":
            case "replace":
                if (operation.Value == null && operation.Path != "/") // Null value is valid for some cases
                {
                    // Allow null values but log warning
                    _logger.LogDebug("Operation {Op} has null value at path {Path}", operation.Op, operation.Path);
                }
                break;

            case "move":
            case "copy":
                if (string.IsNullOrWhiteSpace(operation.From))
                {
                    errors.Add($"'from' field is required for {operation.Op} operation");
                }
                else if (!IsValidJsonPointer(operation.From))
                {
                    errors.Add($"Invalid JSON Pointer in 'from' field: {operation.From}");
                }
                break;

            case "test":
                if (operation.Value == null)
                {
                    errors.Add("'value' field is required for test operation");
                }
                break;

            case "remove":
                // No additional validation needed
                break;
        }

        // 4. Validate userId
        if (string.IsNullOrWhiteSpace(operation.UserId))
        {
            errors.Add("UserId is required");
        }

        // 5. Validate emulationId
        if (string.IsNullOrWhiteSpace(operation.EmulationId))
        {
            errors.Add("EmulationId is required");
        }

        return Task.FromResult(errors.Any()
            ? OperationValidationResult.Failure(errors)
            : OperationValidationResult.Success());
    }

    public async Task<ConflictDetectionResult> DetectConflictsAsync(
        string emulationId,
        EmulationOperation operation,
        long clientLastSeq)
    {
        _logger.LogDebug("Detecting conflicts for emulation: {EmulationId}, clientLastSeq: {ClientSeq}",
            emulationId, clientLastSeq);

        try
        {
            // 1. Get current sequence
            var currentSeq = await _operationRepository.GetLatestSeqAsync(emulationId);

            // 2. No conflict if client is up to date
            if (clientLastSeq >= currentSeq)
            {
                return new ConflictDetectionResult
                {
                    HasConflict = false,
                    ConflictType = ConflictType.None,
                    SuggestedResolution = ConflictResolutionStrategy.Accept
                };
            }

            // 3. Get operations between client seq and current seq
            var missedOperations = await _operationRepository.GetOperationsSinceAsync(emulationId, clientLastSeq);

            if (!missedOperations.Any())
            {
                return new ConflictDetectionResult
                {
                    HasConflict = false,
                    ConflictType = ConflictType.None,
                    SuggestedResolution = ConflictResolutionStrategy.Accept
                };
            }

            // 4. Check if client is too far behind
            if (currentSeq - clientLastSeq > 100)
            {
                return new ConflictDetectionResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.ClientBehind,
                    ConflictingOperations = missedOperations,
                    Message = $"Client is too far behind (missed {missedOperations.Count} operations)",
                    SuggestedResolution = ConflictResolutionStrategy.RejectAndSync
                };
            }

            // 5. Check for same path conflicts
            var conflictingOps = missedOperations
                .Where(op => PathsConflict(operation.Path, op.Path))
                .ToList();

            if (conflictingOps.Any())
            {
                return new ConflictDetectionResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.SamePathModified,
                    ConflictingOperations = conflictingOps,
                    Message = $"Path {operation.Path} was modified by {conflictingOps.Count} recent operation(s)",
                    SuggestedResolution = ConflictResolutionStrategy.LastWriteWins
                };
            }

            // 6. Client is behind but no direct conflicts - use Last-Write-Wins
            return new ConflictDetectionResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ClientBehind,
                ConflictingOperations = missedOperations,
                Message = $"Client is behind by {missedOperations.Count} operation(s) but no path conflicts",
                SuggestedResolution = ConflictResolutionStrategy.LastWriteWins
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect conflicts");
            // On error, allow operation but log warning
            return new ConflictDetectionResult
            {
                HasConflict = false,
                ConflictType = ConflictType.None,
                SuggestedResolution = ConflictResolutionStrategy.Accept,
                Message = $"Conflict detection failed: {ex.Message}"
            };
        }
    }

    public async Task<ApiResult<long>> GetCurrentSequenceAsync(string emulationId)
    {
        try
        {
            var currentSeq = await _operationRepository.GetLatestSeqAsync(emulationId);
            return ApiResult<long>.Succeeded(currentSeq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current sequence for emulation: {EmulationId}", emulationId);
            return ApiResult<long>.Failed($"Failed to get current sequence: {ex.Message}");
        }
    }

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Validate JSON Pointer format (RFC 6901)
    /// </summary>
    private bool IsValidJsonPointer(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Empty string is valid (refers to whole document)
        if (path == "")
            return true;

        // Must start with '/'
        if (!path.StartsWith('/'))
            return false;

        // Check for invalid escape sequences
        // Valid: ~0 (represents ~), ~1 (represents /)
        // Invalid: ~2, ~3, etc.
        if (path.Contains('~'))
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                if (path[i] == '~')
                {
                    var next = path[i + 1];
                    if (next != '0' && next != '1')
                        return false;
                }
            }

            // Check for trailing ~
            if (path.EndsWith('~'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if two JSON Pointer paths conflict
    /// Paths conflict if they are the same or one is parent of the other
    /// </summary>
    private bool PathsConflict(string path1, string path2)
    {
        // Exact match
        if (path1 == path2)
            return true;

        // Check if one is parent of the other
        // e.g., "/components/squares/0" conflicts with "/components/squares/0/rotation/y"
        return path1.StartsWith(path2 + '/') || path2.StartsWith(path1 + '/');
    }
}
