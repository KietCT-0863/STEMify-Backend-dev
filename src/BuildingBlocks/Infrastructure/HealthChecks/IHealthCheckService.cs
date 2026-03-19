namespace Infrastructure.HealthChecks;

public interface IHealthCheckService
{
    string Name { get; }

    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public class HealthCheckResult
{
    public HealthStatus Status { get; set; }

    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object> Data { get; set; } = new();

    public Exception? Exception { get; set; }

    public TimeSpan Duration { get; set; }

    public static HealthCheckResult Healthy(
        string description = "",
        Dictionary<string, object>? data = null
    )
    {
        return new HealthCheckResult
        {
            Status = HealthStatus.Healthy,
            Description = description,
            Data = data ?? new Dictionary<string, object>(),
        };
    }

    public static HealthCheckResult Degraded(
        string description = "",
        Dictionary<string, object>? data = null
    )
    {
        return new HealthCheckResult
        {
            Status = HealthStatus.Degraded,
            Description = description,
            Data = data ?? new Dictionary<string, object>(),
        };
    }

    public static HealthCheckResult Unhealthy(
        string description = "",
        Exception? exception = null,
        Dictionary<string, object>? data = null
    )
    {
        return new HealthCheckResult
        {
            Status = HealthStatus.Unhealthy,
            Description = description,
            Exception = exception,
            Data = data ?? new Dictionary<string, object>(),
        };
    }
}

public enum HealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2,
}
