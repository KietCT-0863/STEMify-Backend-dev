using System.Diagnostics;
using Infrastructure.HealthChecks;
using Microsoft.Extensions.Logging;
using Notification.Application.Common.Interfaces;

namespace Notification.Infrastructure.HealthChecks;

public class NotificationHealthCheck : IHealthCheckService
{
    private readonly INotificationUnitOfWork _unitOfWork;
    private readonly ILogger<NotificationHealthCheck> _logger;

    public string Name => "Notification_Service";

    public NotificationHealthCheck(
        INotificationUnitOfWork unitOfWork,
        ILogger<NotificationHealthCheck> logger
    )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var data = new Dictionary<string, object>();

        try
        {
            await CheckDatabaseConnection(data, cancellationToken);

            await CheckRepositoryFunctionality(data, cancellationToken);

            await CheckNotificationStatistics(data, cancellationToken);

            stopwatch.Stop();
            data["TotalCheckTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogDebug(
                "Notification service health check completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            return HealthCheckResult.Healthy("Notification service is operating normally", data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["TotalCheckTime"] = stopwatch.ElapsedMilliseconds;

            _logger.LogError(
                ex,
                "Notification service health check failed after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            return HealthCheckResult.Unhealthy(
                $"Notification service is experiencing issues: {ex.Message}",
                ex,
                data
            );
        }
    }

    private async Task CheckDatabaseConnection(
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var dbStopwatch = Stopwatch.StartNew();

        try
        {
            await _unitOfWork.Notifications.GetAllAsync(cancellationToken);

            dbStopwatch.Stop();
            data["DatabaseConnectionTime"] = dbStopwatch.ElapsedMilliseconds;
            data["DatabaseStatus"] = "Connected";
        }
        catch (Exception ex)
        {
            dbStopwatch.Stop();
            data["DatabaseConnectionTime"] = dbStopwatch.ElapsedMilliseconds;
            data["DatabaseStatus"] = "Failed";
            data["DatabaseError"] = ex.Message;

            throw new Exception($"Database connection failed: {ex.Message}", ex);
        }
    }

    private async Task CheckRepositoryFunctionality(
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var repoStopwatch = Stopwatch.StartNew();

        try
        {
            var testQuery = await _unitOfWork.Notifications.FindAsync(
                n => n.Id > 0,
                cancellationToken
            );

            repoStopwatch.Stop();
            data["RepositoryFunctionalityTime"] = repoStopwatch.ElapsedMilliseconds;
            data["RepositoryStatus"] = "Working";
        }
        catch (Exception ex)
        {
            repoStopwatch.Stop();
            data["RepositoryFunctionalityTime"] = repoStopwatch.ElapsedMilliseconds;
            data["RepositoryStatus"] = "Failed";
            data["RepositoryError"] = ex.Message;

            throw new Exception($"Repository functionality failed: {ex.Message}", ex);
        }
    }

    private async Task CheckNotificationStatistics(
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    )
    {
        var statsStopwatch = Stopwatch.StartNew();

        try
        {
            var allNotifications = await _unitOfWork.Notifications.GetAllAsync(cancellationToken);
            var totalCount = allNotifications.Count();
            var unreadCount = allNotifications.Count(n => !n.IsRead);
            var todayCount = allNotifications.Count(n =>
                n.CreatedDate.Date == DateTime.UtcNow.Date
            );

            statsStopwatch.Stop();

            data["TotalNotifications"] = totalCount;
            data["UnreadNotifications"] = unreadCount;
            data["TodayNotifications"] = todayCount;
            data["StatisticsTime"] = statsStopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            statsStopwatch.Stop();
            data["StatisticsTime"] = statsStopwatch.ElapsedMilliseconds;
            data["StatisticsError"] = ex.Message;

            _logger.LogWarning(ex, "Failed to get notification statistics during health check");
        }
    }
}
