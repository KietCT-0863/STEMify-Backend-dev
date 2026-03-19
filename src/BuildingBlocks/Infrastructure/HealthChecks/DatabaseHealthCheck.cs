using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.HealthChecks;

public class DatabaseHealthCheck<TDbContext> : IHealthCheckService
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck<TDbContext>> _logger;

    public string Name => $"Database_{typeof(TDbContext).Name}";

    public DatabaseHealthCheck(
        TDbContext dbContext,
        ILogger<DatabaseHealthCheck<TDbContext>> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default
    )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            var connectionString = _dbContext.Database.GetConnectionString();
            var databaseName = _dbContext.Database.GetDbConnection().Database;

            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                { "DatabaseName", databaseName ?? "Unknown" },
                { "ConnectionString", MaskConnectionString(connectionString) },
                { "ResponseTime", stopwatch.ElapsedMilliseconds },
            };

            _logger.LogDebug(
                "Database health check completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            return HealthCheckResult.Healthy("Database connection is working", data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Database health check failed after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds
            );

            var data = new Dictionary<string, object>
            {
                { "ResponseTime", stopwatch.ElapsedMilliseconds },
            };

            return HealthCheckResult.Unhealthy(
                $"Database connection failed: {ex.Message}",
                ex,
                data
            );
        }
    }

    private static string MaskConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Unknown";

        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            var trimmed = part.Trim();
            if (
                trimmed.StartsWith("Password", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Pwd", StringComparison.OrdinalIgnoreCase)
            )
                return "Password=***";
            if (
                trimmed.StartsWith("User Id", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("Username", StringComparison.OrdinalIgnoreCase)
            )
                return "UserId=***";
            return part;
        });

        return string.Join(";", maskedParts);
    }
}
