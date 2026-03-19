namespace Infrastructure.Resilience;

public class PollyResilienceOptions
{
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();
    public RetryConfig Retry { get; set; } = new();
    public TimeoutConfig Timeout { get; set; } = new();
}

public class CircuitBreakerConfig
{
    public int HandledEventsAllowedBeforeBreaking { get; set; } = 5;
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    public int MinimumThroughput { get; set; } = 3;

    public double FailureThreshold { get; set; } = 0.5;

    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(60);
}

public class RetryConfig
{
    public int RetryCount { get; set; } = 3;

    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// thêm jitter để tránh thundering herd không
    /// Default: true
    /// </summary>
    public bool UseJitter { get; set; } = true;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

public class TimeoutConfig
{
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
