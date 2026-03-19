using System.Text.Json;

namespace Infrastructure.Idempotency;

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;

    public string SerializedResult { get; set; } = string.Empty;

    public string ResultType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.Processing;

    public string? ErrorMessage { get; set; }

    public static IdempotencyRecord FromResult<T>(string key, T result, TimeSpan expiration)
    {
        return new IdempotencyRecord
        {
            Key = key,
            SerializedResult = JsonSerializer.Serialize(result),
            ResultType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? "Unknown",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            Status = IdempotencyStatus.Completed,
        };
    }

    public static IdempotencyRecord ForProcessing(string key, TimeSpan expiration)
    {
        return new IdempotencyRecord
        {
            Key = key,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            Status = IdempotencyStatus.Processing,
        };
    }

    public T? GetResult<T>()
    {
        if (string.IsNullOrEmpty(SerializedResult))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(SerializedResult);
        }
        catch
        {
            return default;
        }
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsCompleted => Status == IdempotencyStatus.Completed;
}

public enum IdempotencyStatus
{
    Processing = 0,
    Completed = 1,
    Failed = 2,
}
