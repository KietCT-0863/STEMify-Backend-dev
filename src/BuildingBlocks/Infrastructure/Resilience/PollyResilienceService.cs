using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Collections.Concurrent;

namespace Infrastructure.Resilience;

public class PollyResilienceService : IPollyResilienceService
{
    private readonly PollyResilienceOptions _options;
    private readonly ILogger<PollyResilienceService> _logger;
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines;
    private readonly ConcurrentDictionary<string, object> _genericPipelines;
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakerStates;
    private readonly ConcurrentDictionary<
        string,
        CircuitBreakerStrategyOptions
    > _circuitBreakerOptions;

    public PollyResilienceService(
        IOptions<PollyResilienceOptions> options,
        ILogger<PollyResilienceService> logger
    )
    {
        _options = options.Value;
        _logger = logger;
        _pipelines = new ConcurrentDictionary<string, ResiliencePipeline>();
        _genericPipelines = new ConcurrentDictionary<string, object>();
        _circuitBreakerStates = new ConcurrentDictionary<string, CircuitBreakerState>();
        _circuitBreakerOptions = new ConcurrentDictionary<string, CircuitBreakerStrategyOptions>();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string policyName,
        CancellationToken cancellationToken = default
    )
    {
        var pipeline = GetPipeline<T>(policyName);

        try
        {
            _logger.LogDebug(
                "Executing operation with Polly resilience pipeline: {PolicyName}",
                policyName
            );

            var result = await pipeline.ExecuteAsync(
                async (context, token) =>
                {
                    return await operation(token);
                },
                cancellationToken
            );

            _logger.LogDebug(
                "Operation completed successfully for policy: {PolicyName}",
                policyName
            );
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed for policy: {PolicyName}", policyName);
            throw;
        }
    }

    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        string policyName,
        CancellationToken cancellationToken = default
    )
    {
        var pipeline = GetPipeline(policyName);

        try
        {
            _logger.LogDebug(
                "Executing void operation with Polly resilience pipeline: {PolicyName}",
                policyName
            );

            await pipeline.ExecuteAsync(
                async (context, token) =>
                {
                    await operation(token);
                },
                cancellationToken
            );

            _logger.LogDebug(
                "Void operation completed successfully for policy: {PolicyName}",
                policyName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Void operation failed for policy: {PolicyName}", policyName);
            throw;
        }
    }

    public ResiliencePipeline GetPipeline(string policyName)
    {
        return _pipelines.GetOrAdd(policyName, _ => CreateResiliencePipeline(policyName));
    }

    public ResiliencePipeline<T> GetPipeline<T>(string policyName)
    {
        var key = $"{policyName}_{typeof(T).Name}";
        return (ResiliencePipeline<T>)
            _genericPipelines.GetOrAdd(key, _ => CreateResiliencePipeline<T>(policyName));
    }

    public CircuitBreakerState? GetCircuitBreakerState(string policyName)
    {
        _circuitBreakerStates.TryGetValue(policyName, out var state);
        _logger.LogDebug(
            "Circuit breaker state for policy: {PolicyName} is {State}",
            policyName,
            state
        );
        return state;
    }

    public void ResetCircuitBreaker(string policyName)
    {
        _logger.LogInformation(
            "Circuit breaker reset requested for policy: {PolicyName}",
            policyName
        );

        // Remove existing pipelines to force recreation
        _pipelines.TryRemove(policyName, out _);
        _circuitBreakerStates.TryRemove(policyName, out _);
        _circuitBreakerOptions.TryRemove(policyName, out _);

        // Remove generic pipelines for this policy
        var keysToRemove = _genericPipelines
            .Keys.Where(k => k.StartsWith(policyName + "_"))
            .ToList();
        foreach (var key in keysToRemove)
        {
            _genericPipelines.TryRemove(key, out _);
        }

        _logger.LogInformation(
            "Circuit breaker reset completed for policy: {PolicyName}",
            policyName
        );
    }

    private ResiliencePipeline CreateResiliencePipeline(string policyName)
    {
        _logger.LogDebug(
            "Creating comprehensive resilience pipeline for policy: {PolicyName}",
            policyName
        );

        var builder = new ResiliencePipelineBuilder();

        // Add timeout strategy
        builder.AddTimeout(
            new TimeoutStrategyOptions
            {
                Timeout = _options.Timeout.OperationTimeout,
                OnTimeout = args =>
                {
                    _logger.LogWarning(
                        "Operation timeout for policy: {PolicyName} after {Timeout}ms",
                        policyName,
                        _options.Timeout.OperationTimeout.TotalMilliseconds
                    );
                    return ValueTask.CompletedTask;
                },
            }
        );

        // Add retry strategy
        builder.AddRetry(
            new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.Retry.RetryCount,
                BackoffType = _options.Retry.UseExponentialBackoff
                    ? DelayBackoffType.Exponential
                    : DelayBackoffType.Constant,
                UseJitter = _options.Retry.UseJitter,
                Delay = _options.Retry.BaseDelay,
                MaxDelay = _options.Retry.MaxDelay,
                ShouldHandle = args =>
                {
                    // Retry on transient exceptions
                    var shouldRetry = args.Outcome.Exception switch
                    {
                        HttpRequestException => true,
                        TaskCanceledException => false, // Don't retry cancellation
                        OperationCanceledException => false, // Don't retry cancellation
                        TimeoutException => true,
                        _ => args.Outcome.Exception is not ArgumentException, // Don't retry argument exceptions
                    };
                    return ValueTask.FromResult(shouldRetry);
                },
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry attempt {Attempt} for policy: {PolicyName} due to: {Exception}",
                        args.AttemptNumber,
                        policyName,
                        args.Outcome.Exception?.Message
                    );
                    return ValueTask.CompletedTask;
                },
            }
        );

        // Add circuit breaker strategy
        var circuitBreakerOptions = CreateCircuitBreakerOptions(policyName);
        builder.AddCircuitBreaker(circuitBreakerOptions);

        return builder.Build();
    }

    private ResiliencePipeline<T> CreateResiliencePipeline<T>(string policyName)
    {
        _logger.LogDebug(
            "Creating simplified generic resilience pipeline for policy: {PolicyName}, type: {Type}",
            policyName,
            typeof(T).Name
        );

        var builder = new ResiliencePipelineBuilder<T>();

        // Add timeout strategy
        builder.AddTimeout(
            new TimeoutStrategyOptions
            {
                Timeout = _options.Timeout.OperationTimeout,
                OnTimeout = args =>
                {
                    _logger.LogWarning(
                        "Generic operation timeout for policy: {PolicyName} after {Timeout}ms",
                        policyName,
                        _options.Timeout.OperationTimeout.TotalMilliseconds
                    );
                    return ValueTask.CompletedTask;
                },
            }
        );

        return builder.Build();
    }

    private CircuitBreakerStrategyOptions CreateCircuitBreakerOptions(string policyName)
    {
        var options = new CircuitBreakerStrategyOptions
        {
            FailureRatio = _options.CircuitBreaker.FailureThreshold,
            MinimumThroughput = _options.CircuitBreaker.MinimumThroughput,
            SamplingDuration = _options.CircuitBreaker.SamplingDuration,
            BreakDuration = _options.CircuitBreaker.DurationOfBreak,
            ShouldHandle = args =>
            {
                // Don't break circuit on cancellation
                if (args.Outcome.Exception is OperationCanceledException or TaskCanceledException)
                    return ValueTask.FromResult(false);

                // Break circuit on specific exceptions
                var shouldBreak = args.Outcome.Exception switch
                {
                    HttpRequestException => true,
                    TimeoutException => true,
                    _ => args.Outcome.Exception is not ArgumentException, // Don't break on argument exceptions
                };
                return ValueTask.FromResult(shouldBreak);
            },
            OnOpened = args =>
            {
                _circuitBreakerStates[policyName] = CircuitBreakerState.Open;
                _logger.LogWarning("Circuit breaker opened for policy: {PolicyName}", policyName);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                _circuitBreakerStates[policyName] = CircuitBreakerState.Closed;
                _logger.LogInformation(
                    "Circuit breaker closed for policy: {PolicyName}",
                    policyName
                );
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                _circuitBreakerStates[policyName] = CircuitBreakerState.HalfOpen;
                _logger.LogInformation(
                    "Circuit breaker half-opened for policy: {PolicyName}",
                    policyName
                );
                return ValueTask.CompletedTask;
            },
        };

        _circuitBreakerOptions[policyName] = options;
        _circuitBreakerStates[policyName] = CircuitBreakerState.Closed;

        return options;
    }
}
