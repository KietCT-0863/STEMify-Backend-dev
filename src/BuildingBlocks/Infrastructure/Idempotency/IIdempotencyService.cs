namespace Infrastructure.Idempotency;

public interface IIdempotencyService
{
    Task<T> ExecuteAsync<T>(
        string idempotencyKey,
        Func<CancellationToken, Task<T>> operation,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<T?> GetResultAsync<T>(
        string idempotencyKey,
        CancellationToken cancellationToken = default
    );
    Task RemoveAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}
