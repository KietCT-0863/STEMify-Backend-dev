using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired invitations
/// Runs every 6 hours to mark expired invitations as Expired
/// </summary>
public class ExpiredInvitationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredInvitationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6);

    public ExpiredInvitationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredInvitationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredInvitationCleanupService started");

        try
        {
            await WaitForDatabaseInitializationAsync(stoppingToken);

            // Run once on startup after DB is ready
            await CleanupExpiredInvitationsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    await CleanupExpiredInvitationsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExpiredInvitationCleanupService cleanup cycle. Will retry next interval.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ExpiredInvitationCleanupService cancelled during startup");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in ExpiredInvitationCleanupService. Service will stop but app will continue.");
        }

        _logger.LogInformation("ExpiredInvitationCleanupService stopped");
    }

    private async Task WaitForDatabaseInitializationAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbInitService = scope.ServiceProvider.GetService<IDatabaseInitializationService>();

        if (dbInitService == null)
        {
            return;
        }

        try
        {
            
            await dbInitService.WaitUntilInitializedAsync(cancellationToken);
            }
        
        catch (Exception ex)
        {
            }
    }

    private async Task CleanupExpiredInvitationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var invitationRepository = scope.ServiceProvider.GetRequiredService<IInvitationRepository>();

        try
        {
            _logger.LogInformation("Starting cleanup of expired invitations");

            var expiredInvitations = await invitationRepository.GetExpiredInvitationsAsync(cancellationToken);

            if (!expiredInvitations.Any())
            {
                _logger.LogInformation("No expired invitations found");
                return;
            }

            _logger.LogInformation("Found {Count} expired invitations to mark", expiredInvitations.Count);

            var markedCount = 0;
            foreach (var invitation in expiredInvitations)
            {
                try
                {
                    invitation.MarkAsExpired();
                    await invitationRepository.UpdateAsync(invitation, cancellationToken);
                    markedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to mark invitation {InvitationId} as expired",
                        invitation.Id);
                }
            }

            _logger.LogInformation(
                "Cleanup completed: {MarkedCount} invitations marked as expired",
                markedCount);
        }
        catch (PostgresException pg) when (pg.SqlState == "42P01")
        {
            _logger.LogWarning(pg, "Invitations table not found during cleanup. Will retry on next interval.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during expired invitation cleanup. Will retry on next interval.");
        }
    }
}
