using Identity.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Identity.Web.Services;

/// <summary>
/// Service to track database initialization status for health checks
/// </summary>
public class DatabaseInitializationService : IDatabaseInitializationService
{
    private volatile bool _isInitialized = false;
    private Exception? _initializationException = null;
    private readonly TaskCompletionSource<bool> _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsInitialized => _isInitialized;
    public Exception? InitializationException => _initializationException;

    public void MarkAsInitialized()
    {
        _isInitialized = true;
        _initializationException = null;
        _readyTcs.TrySetResult(true);
    }

    public void MarkAsFailed(Exception exception)
    {
        _isInitialized = false;
        _initializationException = exception;
        _readyTcs.TrySetException(exception);
    }

    public async Task WaitUntilInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        if (cancellationToken.CanBeCanceled)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            
            try
            {
                await Task.WhenAny(_readyTcs.Task, tcs.Task);
            }
            finally
            {
                registration.Dispose();
            }
            cancellationToken.ThrowIfCancellationRequested();
            await _readyTcs.Task;
        }
        else
        {
            await _readyTcs.Task;
        }
    }
}

/// <summary>
/// Custom health check that waits for database initialization to complete
/// </summary>
public class DatabaseInitializationHealthCheck : IHealthCheck
{
    private readonly DatabaseInitializationService _dbInitService;

    public DatabaseInitializationHealthCheck(DatabaseInitializationService dbInitService)
    {
        _dbInitService = dbInitService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (_dbInitService.IsInitialized)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Database initialization completed successfully")
            );
        }

        if (_dbInitService.InitializationException != null)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Database initialization failed",
                    _dbInitService.InitializationException
                )
            );
        }

        return Task.FromResult(HealthCheckResult.Degraded("Database initialization in progress"));
    }
}
