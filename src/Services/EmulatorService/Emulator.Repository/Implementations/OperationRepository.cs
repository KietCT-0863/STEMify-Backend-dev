using Emulator.Repository.Entities;
using Emulator.Repository.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Emulator.Repository.Implementations;

/// <summary>
/// Provides atomic sequence number generation and efficient querying
/// </summary>
public class OperationRepository : IOperationRepository
{
    private readonly IMongoCollection<EmulationOperation> _operations;
    private readonly IMongoCollection<Emulation> _emulations;
    private readonly ILogger<OperationRepository> _logger;

    public OperationRepository(
        IMongoDatabase database,
        ILogger<OperationRepository> logger)
    {
        _operations = database.GetCollection<EmulationOperation>("emulation_operations");
        _emulations = database.GetCollection<Emulation>("emulations");
        _logger = logger;

        // Ensure indexes exist
        EnsureIndexes();
    }

    /// <summary>
    /// Create MongoDB indexes for optimal performance
    /// </summary>
    private void EnsureIndexes()
    {
        try
        {
            // Composite index for emulationId + seq (unique, for ordering)
            var emulationSeqIndex = Builders<EmulationOperation>.IndexKeys
                .Ascending(x => x.EmulationId)
                .Ascending(x => x.Seq);
            _operations.Indexes.CreateOne(new CreateIndexModel<EmulationOperation>(
                emulationSeqIndex,
                new CreateIndexOptions { Unique = true, Name = "idx_emulation_seq" }));

            // Index for querying by appliedToSnapshot status
            var emulationAppliedIndex = Builders<EmulationOperation>.IndexKeys
                .Ascending(x => x.EmulationId)
                .Ascending(x => x.AppliedToSnapshot);
            _operations.Indexes.CreateOne(new CreateIndexModel<EmulationOperation>(
                emulationAppliedIndex,
                new CreateIndexOptions { Name = "idx_emulation_applied" }));

            // Index for timestamp (with TTL for automatic cleanup after 30 days)
            var timestampIndex = Builders<EmulationOperation>.IndexKeys
                .Ascending(x => x.Timestamp);
            _operations.Indexes.CreateOne(new CreateIndexModel<EmulationOperation>(
                timestampIndex,
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromDays(30),
                    Name = "idx_timestamp_ttl"
                }));

            // Index for user analytics
            var userTimestampIndex = Builders<EmulationOperation>.IndexKeys
                .Ascending(x => x.UserId)
                .Descending(x => x.Timestamp);
            _operations.Indexes.CreateOne(new CreateIndexModel<EmulationOperation>(
                userTimestampIndex,
                new CreateIndexOptions { Name = "idx_user_timestamp" }));

            // Index for batch operations
            var batchIndex = Builders<EmulationOperation>.IndexKeys
                .Ascending(x => x.EmulationId)
                .Ascending("metadata.batchId");
            _operations.Indexes.CreateOne(new CreateIndexModel<EmulationOperation>(
                batchIndex,
                new CreateIndexOptions { Name = "idx_emulation_batch", Sparse = true }));

            _logger.LogInformation("Operation repository indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes (may already exist)");
        }
    }

    public async Task<EmulationOperation> AppendOperationAsync(EmulationOperation operation)
    {
        _logger.LogDebug("Appending operation to emulation: {EmulationId}", operation.EmulationId);

        try
        {
            // 1. Atomic increment of sequence number using findAndModify
            var filter = Builders<Emulation>.Filter.Eq(x => x.EmulationId, operation.EmulationId);
            var update = Builders<Emulation>.Update
                .Inc(x => x.CurrentSeq, 1)
                .Inc(x => x.TotalOperations, 1)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var options = new FindOneAndUpdateOptions<Emulation, Emulation>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false // Emulation must exist
            };

            var emulation = await _emulations.FindOneAndUpdateAsync(filter, update, options);

            if (emulation == null)
            {
                throw new InvalidOperationException($"Emulation not found: {operation.EmulationId}");
            }

            // 2. Set sequence number and timestamp
            operation.Seq = emulation.CurrentSeq;
            operation.Timestamp = DateTime.UtcNow;
            operation.AppliedToSnapshot = false;

            // 3. Insert operation
            await _operations.InsertOneAsync(operation);

            _logger.LogDebug("Operation appended: {EmulationId}, seq: {Seq}", operation.EmulationId, operation.Seq);

            return operation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append operation to emulation: {EmulationId}", operation.EmulationId);
            throw;
        }
    }

    public async Task<List<EmulationOperation>> AppendBatchAsync(List<EmulationOperation> operations)
    {
        if (operations == null || operations.Count == 0)
        {
            return new List<EmulationOperation>();
        }

        var emulationId = operations[0].EmulationId;
        _logger.LogInformation("Appending batch of {Count} operations to emulation: {EmulationId}",
            operations.Count, emulationId);

        try
        {
            // Use MongoDB transaction for atomicity
            using var session = await _operations.Database.Client.StartSessionAsync();
            session.StartTransaction();

            try
            {
                // 1. Atomic increment of sequence number by batch size
                var filter = Builders<Emulation>.Filter.Eq(x => x.EmulationId, emulationId);
                var update = Builders<Emulation>.Update
                    .Inc(x => x.CurrentSeq, operations.Count)
                    .Inc(x => x.TotalOperations, operations.Count)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);

                var options = new FindOneAndUpdateOptions<Emulation, Emulation>
                {
                    ReturnDocument = ReturnDocument.After,
                    IsUpsert = false
                };

                var emulation = await _emulations.FindOneAndUpdateAsync(session, filter, update, options);

                if (emulation == null)
                {
                    throw new InvalidOperationException($"Emulation not found: {emulationId}");
                }

                // 2. Assign sequence numbers and timestamps
                var startSeq = emulation.CurrentSeq - operations.Count + 1;
                var timestamp = DateTime.UtcNow;

                for (int i = 0; i < operations.Count; i++)
                {
                    operations[i].Seq = startSeq + i;
                    operations[i].Timestamp = timestamp;
                    operations[i].AppliedToSnapshot = false;
                }

                // 3. Insert all operations
                await _operations.InsertManyAsync(session, operations);

                // 4. Commit transaction
                await session.CommitTransactionAsync();

                _logger.LogInformation("Batch appended: {EmulationId}, seqs: {StartSeq}-{EndSeq}",
                    emulationId, startSeq, emulation.CurrentSeq);

                return operations;
            }
            catch
            {
                await session.AbortTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append batch to emulation: {EmulationId}", emulationId);
            throw;
        }
    }

    public async Task<List<EmulationOperation>> GetOperationsSinceAsync(
        string emulationId,
        long sinceSeq,
        bool includeDeleted = false)
    {
        _logger.LogDebug("Getting operations since seq {Seq} for emulation: {EmulationId}", sinceSeq, emulationId);

        var filterBuilder = Builders<EmulationOperation>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(x => x.EmulationId, emulationId),
            filterBuilder.Gt(x => x.Seq, sinceSeq)
        );

        if (!includeDeleted)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.IsDeleted, false));
        }

        return await _operations.Find(filter)
            .SortBy(x => x.Seq)
            .ToListAsync();
    }

    public async Task<List<EmulationOperation>> GetOperationsRangeAsync(
        string emulationId,
        long fromSeq,
        long toSeq,
        bool includeDeleted = false)
    {
        _logger.LogDebug("Getting operations range {From}-{To} for emulation: {EmulationId}",
            fromSeq, toSeq, emulationId);

        var filterBuilder = Builders<EmulationOperation>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(x => x.EmulationId, emulationId),
            filterBuilder.Gte(x => x.Seq, fromSeq),
            filterBuilder.Lte(x => x.Seq, toSeq)
        );

        if (!includeDeleted)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.IsDeleted, false));
        }

        return await _operations.Find(filter)
            .SortBy(x => x.Seq)
            .ToListAsync();
    }

    public async Task<List<EmulationOperation>> GetOperationsByBatchAsync(string emulationId, string batchId)
    {
        _logger.LogDebug("Getting operations for batch: {BatchId} in emulation: {EmulationId}",
            batchId, emulationId);

        var filter = Builders<EmulationOperation>.Filter.And(
            Builders<EmulationOperation>.Filter.Eq(x => x.EmulationId, emulationId),
            Builders<EmulationOperation>.Filter.Eq("metadata.batchId", batchId),
            Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
        );

        return await _operations.Find(filter)
            .SortBy(x => x.Seq)
            .ToListAsync();
    }

    public async Task<long> GetLatestSeqAsync(string emulationId)
    {
        var emulation = await _emulations.Find(x => x.EmulationId == emulationId)
            .Project(x => x.CurrentSeq)
            .FirstOrDefaultAsync();

        return emulation;
    }

    public async Task<int> MarkOperationsAsAppliedAsync(
        string emulationId,
        long snapshotSeq,
        List<long> operationSeqs)
    {
        _logger.LogInformation("Marking {Count} operations as applied to snapshot {Seq} for emulation: {EmulationId}",
            operationSeqs.Count, snapshotSeq, emulationId);

        var filter = Builders<EmulationOperation>.Filter.And(
            Builders<EmulationOperation>.Filter.Eq(x => x.EmulationId, emulationId),
            Builders<EmulationOperation>.Filter.In(x => x.Seq, operationSeqs)
        );

        var update = Builders<EmulationOperation>.Update
            .Set(x => x.AppliedToSnapshot, true)
            .Set(x => x.SnapshotSeq, snapshotSeq);

        var result = await _operations.UpdateManyAsync(filter, update);

        return (int)result.ModifiedCount;
    }

    public async Task<int> PruneOperationsAsync(
        string emulationId,
        long beforeSeq,
        bool onlyApplied = true)
    {
        _logger.LogInformation("Pruning operations before seq {Seq} for emulation: {EmulationId} (onlyApplied: {OnlyApplied})",
            beforeSeq, emulationId, onlyApplied);

        var filterBuilder = Builders<EmulationOperation>.Filter;
        var filter = filterBuilder.And(
            filterBuilder.Eq(x => x.EmulationId, emulationId),
            filterBuilder.Lt(x => x.Seq, beforeSeq),
            filterBuilder.Eq(x => x.IsDeleted, false) // Only soft delete once
        );

        if (onlyApplied)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.AppliedToSnapshot, true));
        }

        var update = Builders<EmulationOperation>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow);

        var result = await _operations.UpdateManyAsync(filter, update);

        _logger.LogInformation("Pruned {Count} operations", result.ModifiedCount);

        return (int)result.ModifiedCount;
    }

    public async Task<int> HardDeleteOperationsAsync(string emulationId, long beforeSeq)
    {
        _logger.LogWarning("HARD DELETING operations before seq {Seq} for emulation: {EmulationId}",
            beforeSeq, emulationId);

        var filter = Builders<EmulationOperation>.Filter.And(
            Builders<EmulationOperation>.Filter.Eq(x => x.EmulationId, emulationId),
            Builders<EmulationOperation>.Filter.Lt(x => x.Seq, beforeSeq)
        );

        var result = await _operations.DeleteManyAsync(filter);

        _logger.LogWarning("Hard deleted {Count} operations permanently", result.DeletedCount);

        return (int)result.DeletedCount;
    }

    public async Task<OperationStatistics> GetStatisticsAsync(string emulationId)
    {
        _logger.LogDebug("Getting statistics for emulation: {EmulationId}", emulationId);

        // Get emulation for snapshot info
        var emulation = await _emulations.Find(x => x.EmulationId == emulationId)
            .FirstOrDefaultAsync();

        if (emulation == null)
        {
            throw new InvalidOperationException($"Emulation not found: {emulationId}");
        }

        // Count operations by status
        var filter = Builders<EmulationOperation>.Filter.Eq(x => x.EmulationId, emulationId);

        var totalOps = await _operations.CountDocumentsAsync(filter);

        var appliedOps = await _operations.CountDocumentsAsync(
            Builders<EmulationOperation>.Filter.And(
                filter,
                Builders<EmulationOperation>.Filter.Eq(x => x.AppliedToSnapshot, true),
                Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
            ));

        var deletedOps = await _operations.CountDocumentsAsync(
            Builders<EmulationOperation>.Filter.And(
                filter,
                Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, true)
            ));

        // Get last operation timestamp
        var lastOp = await _operations.Find(filter)
            .SortByDescending(x => x.Timestamp)
            .Limit(1)
            .FirstOrDefaultAsync();

        // Get operations by type
        var opsByType = await _operations.Aggregate()
            .Match(Builders<EmulationOperation>.Filter.And(
                filter,
                Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
            ))
            .Group(x => x.Op, g => new { Op = g.Key, Count = g.Count() })
            .ToListAsync();

        // Get operations by user
        var opsByUser = await _operations.Aggregate()
            .Match(Builders<EmulationOperation>.Filter.And(
                filter,
                Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
            ))
            .Group(x => x.UserId, g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Calculate average ops per day (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var recentOps = await _operations.CountDocumentsAsync(
            Builders<EmulationOperation>.Filter.And(
                filter,
                Builders<EmulationOperation>.Filter.Gte(x => x.Timestamp, sevenDaysAgo),
                Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
            ));

        var stats = new OperationStatistics
        {
            TotalOperations = totalOps,
            PendingOperations = totalOps - appliedOps - deletedOps,
            AppliedOperations = appliedOps,
            DeletedOperations = deletedOps,
            LastSeq = emulation.CurrentSeq,
            LastSnapshotSeq = emulation.LastSnapshotSeq,
            LastOperationAt = lastOp?.Timestamp,
            LastSnapshotAt = emulation.LastSnapshotAt,
            OperationsByType = opsByType.ToDictionary(x => x.Op, x => (long)x.Count),
            OperationsByUser = opsByUser.ToDictionary(x => x.UserId, x => (long)x.Count),
            AverageOperationsPerDay = recentOps / 7.0
        };

        _logger.LogDebug("Statistics for {EmulationId}: {Summary}", emulationId, stats.GetSummary());

        return stats;
    }

    public async Task<long> CountOperationsAsync(string emulationId, bool includeDeleted = false)
    {
        var filterBuilder = Builders<EmulationOperation>.Filter;
        var filter = filterBuilder.Eq(x => x.EmulationId, emulationId);

        if (!includeDeleted)
        {
            filter = filterBuilder.And(filter, filterBuilder.Eq(x => x.IsDeleted, false));
        }

        return await _operations.CountDocumentsAsync(filter);
    }

    public async Task<bool> HasOperationsAsync(string emulationId)
    {
        var filter = Builders<EmulationOperation>.Filter.And(
            Builders<EmulationOperation>.Filter.Eq(x => x.EmulationId, emulationId),
            Builders<EmulationOperation>.Filter.Eq(x => x.IsDeleted, false)
        );

        return await _operations.Find(filter).AnyAsync();
    }
}
