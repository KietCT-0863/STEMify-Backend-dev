using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Infrastructure.Idempotency;

public class InMemoryIdempotencyService : IIdempotencyService, IDisposable
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records;
    private readonly ILogger<InMemoryIdempotencyService> _logger;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;

    public InMemoryIdempotencyService(ILogger<InMemoryIdempotencyService> logger)
    {
        _records = new ConcurrentDictionary<string, IdempotencyRecord>();
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<T> ExecuteAsync<T>(
        string idempotencyKey,
        Func<CancellationToken, Task<T>> operation,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        var exp = expiration ?? TimeSpan.FromHours(1);

        // Use semaphore to synchronize access to the critical section
        await _semaphore.WaitAsync(cancellationToken);
        IdempotencyRecord? processingRecord = null;
        try
        {
            if (_records.TryGetValue(idempotencyKey, out var existingRecord))
            {
                if (existingRecord.IsExpired)
                {
                    _records.TryRemove(idempotencyKey, out _);
                    _logger.LogDebug(
                        "Expired idempotency record removed for key: {Key}",
                        idempotencyKey
                    );
                }
                else if (existingRecord.IsCompleted)
                {
                    _logger.LogDebug(
                        "Returning cached result for idempotency key: {Key}",
                        idempotencyKey
                    );
                    return existingRecord.GetResult<T>() ?? default!;
                }
                else
                {
                    _logger.LogWarning(
                        "Operation already in progress for idempotency key: {Key}",
                        idempotencyKey
                    );
                    throw new InvalidOperationException(
                        $"Operation with key '{idempotencyKey}' is already in progress"
                    );
                }
            }

            processingRecord = IdempotencyRecord.ForProcessing(idempotencyKey, exp);

            if (!_records.TryAdd(idempotencyKey, processingRecord))
            {
                _logger.LogWarning(
                    "Race condition detected for idempotency key: {Key}",
                    idempotencyKey
                );
                throw new InvalidOperationException(
                    $"Operation with key '{idempotencyKey}' is already in progress"
                );
            }

            // Release semaphore before executing the operation to avoid blocking other operations
            _semaphore.Release();
        }
        catch
        {
            // Ensure semaphore is released even if an exception occurs
            _semaphore.Release();
            throw;
        }

        try
        {
            _logger.LogDebug("Executing operation for idempotency key: {Key}", idempotencyKey);

            var result = await operation(cancellationToken);

            var completedRecord = IdempotencyRecord.FromResult(idempotencyKey, result, exp);
            _records.TryUpdate(idempotencyKey, completedRecord, processingRecord);

            _logger.LogDebug(
                "Operation completed and cached for idempotency key: {Key}",
                idempotencyKey
            );

            return result;
        }
        catch (Exception ex)
        {
            _records.TryRemove(idempotencyKey, out _);

            _logger.LogError(ex, "Operation failed for idempotency key: {Key}", idempotencyKey);

            throw;
        }
    }

    public Task<bool> ExistsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        var exists =
            _records.TryGetValue(idempotencyKey, out var record)
            && record != null
            && !record.IsExpired;

        return Task.FromResult(exists);
    }

    public Task<T?> GetResultAsync<T>(
        string idempotencyKey,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        if (
            _records.TryGetValue(idempotencyKey, out var record)
            && !record.IsExpired
            && record.IsCompleted
        )
        {
            return Task.FromResult(record.GetResult<T>());
        }

        return Task.FromResult<T?>(default);
    }

    public Task RemoveAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _records.TryRemove(idempotencyKey, out _);
        _logger.LogDebug("Idempotency record removed for key: {Key}", idempotencyKey);

        return Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InMemoryIdempotencyService));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
