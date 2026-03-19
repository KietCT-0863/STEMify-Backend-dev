using Polly;

namespace Infrastructure.Resilience;

public interface IPollyResilienceService
{
    /// <summary>
    /// Thực thi operation với full resilience protection (Circuit Breaker + Retry + Timeout)
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
    /// <param name="operation">Operation cần thực thi</param>
    /// <param name="policyName">Tên policy để tracking và configuration riêng biệt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Kết quả của operation</returns>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        string policyName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Thực thi operation không có return value với full resilience protection
    /// </summary>
    /// <param name="operation">Operation cần thực thi</param>
    /// <param name="policyName">Tên policy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        string policyName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lấy resilience pipeline cho một policy name cụ thể
    /// </summary>
    /// <param name="policyName">Tên policy</param>
    /// <returns>Resilience pipeline</returns>
    ResiliencePipeline GetPipeline(string policyName);

    /// <summary>
    /// Lấy resilience pipeline generic cho một policy name cụ thể
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
    /// <param name="policyName">Tên policy</param>
    /// <returns>Resilience pipeline</returns>
    ResiliencePipeline<T> GetPipeline<T>(string policyName);

    /// <summary>
    /// Kiểm tra trạng thái circuit breaker cho một policy
    /// </summary>
    /// <param name="policyName">Tên policy</param>
    /// <returns>Trạng thái circuit (nếu có)</returns>
    CircuitBreakerState? GetCircuitBreakerState(string policyName);

    /// <summary>
    /// Reset circuit breaker về trạng thái closed
    /// </summary>
    /// <param name="policyName">Tên policy</param>
    void ResetCircuitBreaker(string policyName);
}

/// <summary>
/// Trạng thái của Circuit Breaker từ Polly
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen,
    Isolated,
}
