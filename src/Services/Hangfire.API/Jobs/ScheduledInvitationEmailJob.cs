using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Hangfire.API.Jobs;

/// <summary>
/// Hangfire job that sends scheduled invitation emails
/// Runs daily at 9:00 AM UTC to send invitations scheduled for that day
/// </summary>
public class ScheduledInvitationEmailJob
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IInvitationEmailService _emailService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<ScheduledInvitationEmailJob> _logger;

    public ScheduledInvitationEmailJob(
        IInvitationRepository invitationRepository,
        IInvitationEmailService emailService,
        IIdentityUnitOfWork unitOfWork,
        ILogger<ScheduledInvitationEmailJob> logger)
    {
        _invitationRepository = invitationRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SendScheduledInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        

        try
        {
            // Get all invitations scheduled for today
            var scheduledInvitations = await _invitationRepository
                .GetScheduledInvitationsForDateAsync(today, cancellationToken);

            if (!scheduledInvitations.Any())
            {
                return;
            }


            var successCount = 0;
            var failureCount = 0;

            foreach (var invitation in scheduledInvitations)
            {
                try
                {
                    if (invitation.ScheduledSendDate.HasValue && 
                        invitation.ScheduledSendDate.Value.Date > today)
                    {
                        continue;
                    }

                    if (invitation.Status != InvitationStatus.Pending)
                    {
                        continue;
                    }

                    if (invitation.IsExpired())
                    {
                        invitation.MarkAsExpired();
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        continue;
                    }

                    await _emailService.SendInvitationEmailAsync(
                        invitation,
                        invitation.OrganizationId,
                        cancellationToken);

                    invitation.MarkAsSent();
                    successCount++;

                }
                catch (Exception ex)
                {
                    failureCount++;
                    invitation.MarkAsFailed($"Scheduled email send failed: {ex.Message}");
                }
            }

            // Save all changes (both successes and failures)
            if (successCount > 0 || failureCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in scheduled invitation email job for {Date}",
                today);
            throw;
        }
    }
}

