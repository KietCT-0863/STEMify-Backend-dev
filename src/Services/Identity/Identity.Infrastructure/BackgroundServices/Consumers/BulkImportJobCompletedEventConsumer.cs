using Contracts.Abstractions.Services;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Consumer that handles bulk import job completion
/// Sends summary email to job creator
/// </summary>
public class BulkImportJobCompletedEventConsumer : IConsumer<BulkImportJobCompletedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IBulkImportJobRepository _jobRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BulkImportJobCompletedEventConsumer> _logger;

    public BulkImportJobCompletedEventConsumer(
        IEmailService emailService,
        IBulkImportJobRepository jobRepository,
        IUserRepository userRepository,
        ILogger<BulkImportJobCompletedEventConsumer> logger)
    {
        _emailService = emailService;
        _jobRepository = jobRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BulkImportJobCompletedEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing BulkImportJobCompletedEvent for job {JobId}: {SuccessCount} succeeded, {FailedCount} failed",
            @event.JobId,
            @event.SuccessCount,
            @event.FailedCount);

        try
        {
            // Send completion notification email
            await SendCompletionEmailAsync(@event);

            _logger.LogInformation(
                "Completion notification sent for job {JobId}",
                @event.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing BulkImportJobCompletedEvent for job {JobId}",
                @event.JobId);

            // Don't throw - email failure shouldn't retry
        }
    }

    private async Task SendCompletionEmailAsync(BulkImportJobCompletedEvent @event)
    {
        try
        {
            var successRate = @event.SuccessRate;
            var statusIcon = @event.FailedCount == 0 ? "✅" : "⚠️";

            var emailBody = $@"
                <h2>{statusIcon} Bulk Invitation Job Completed</h2>
                <p>Your bulk invitation job has finished processing.</p>

                <h3>Summary:</h3>
                <ul>
                    <li><strong>Total Invitations:</strong> {@event.TotalCount}</li>
                    <li><strong>Successfully Sent:</strong> {@event.SuccessCount}</li>
                    <li><strong>Failed:</strong> {@event.FailedCount}</li>
                    <li><strong>Success Rate:</strong> {successRate:F1}%</li>
                    <li><strong>Duration:</strong> {@event.Duration.ToString(@"hh\:mm\:ss")}</li>
                </ul>

                {GetFailuresSection(@event)}

                <p>You can view detailed results in your organization dashboard.</p>
            ";
            // Resolve creator email from job.CreatedBy
            var job = await _jobRepository.FindByIdAsync(@event.JobId);
            if (job == null)
            {
                _logger.LogWarning("Job {JobId} not found while sending completion email", @event.JobId);
                return;
            }

            var creator = await _userRepository.GetByIdAsync(job.CreatedBy);
            if (creator == null || string.IsNullOrWhiteSpace(creator.Email))
            {
                _logger.LogWarning("Creator {CreatorId} not found or has no email for job {JobId}", job.CreatedBy, @event.JobId);
                return;
            }

            var creatorEmail = creator.Email;

            await _emailService.SendEmailAsync(
                to: creatorEmail,
                subject: $"Bulk Invitation Job Completed - {@event.SuccessCount}/{@event.TotalCount} Successful",
                body: emailBody,
                isHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send completion email for job {JobId}",
                @event.JobId);
        }
    }

    private static string GetFailuresSection(BulkImportJobCompletedEvent @event)
    {
        if (@event.FailedCount == 0)
        {
            return "<p style='color: green;'>All invitations were sent successfully!</p>";
        }

        return $@"
            <h3 style='color: orange;'>Failed Invitations:</h3>
            <p>{@event.FailedCount} invitation(s) could not be sent. Common reasons:</p>
            <ul>
                <li>Invalid email addresses</li>
                <li>Email service temporarily unavailable</li>
                <li>User already has pending invitation</li>
            </ul>
            <p>Please review the failed invitations in the dashboard and resend if needed.</p>
        ";
    }
}
