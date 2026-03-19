using System;
using System.Collections.Generic;
using System.Linq;
using EventBus.Messages.Subscription;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Notification.Application.Commands;
using Notification.Application.Common.Configurations;
using Notification.Application.Common.Hubs;
using Notification.Application.Common.Interfaces.Services;
using Notification.Infrastructure.Helpers;

namespace Notification.API.Consumers
{
    public class SubscriptionExpiryWarningConsumer : IConsumer<SubscriptionExpiryWarningEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<SubscriptionExpiryWarningConsumer> _logger;
        private readonly Contracts.Abstractions.Services.IEmailService _emailService;
        private readonly ClientAppSettings _clientAppSettings;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SubscriptionExpiryWarningConsumer(
            IMediator mediator,
            ILogger<SubscriptionExpiryWarningConsumer> logger,
            Contracts.Abstractions.Services.IEmailService emailService,
            IOptions<ClientAppSettings> clientAppSettings,
            IHubContext<NotificationHub> hubContext)
        {
            _mediator = mediator;
            _logger = logger;
            _emailService = emailService;
            _clientAppSettings = clientAppSettings.Value;
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<SubscriptionExpiryWarningEvent> context)
        {
            var @event = context.Message;

            _logger.LogInformation(
                "SubscriptionExpiryWarningConsumer: Processing expiry warning for Organization {OrganizationId}, " +
                "Subscription {SubscriptionId}, expires in {Days} days",
                @event.OrganizationId,
                @event.SubscriptionOrderId,
                @event.DaysUntilExpiry);

            var adminUserIds = (@event.AdminUserIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            var adminEmails = (@event.AdminEmails ?? new List<string>())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!adminUserIds.Any() && !adminEmails.Any())
            {
                _logger.LogWarning(
                    "SubscriptionExpiryWarningConsumer: No organization admins found for Organization {OrganizationId}. " +
                    "Skipping notification.",
                    @event.OrganizationId);
                return;
            }

            // Create in-app notifications for each admin
            //foreach (var adminUserId in adminUserIds)
            //{
            //    try
            //    {
            //        var notificationCommand = new CreateNotificationCommand
            //        {
            //            Title = "Subscription Expiring Soon",
            //            UserId = adminUserId,
            //            Message = $"Your subscription for **{@event.OrganizationName}** " +
            //                      $"({@event.PlanName}) will expire in **{@event.DaysUntilExpiry} days** " +
            //                      $"on {                    @event.ExpiryDate:MMMM dd, yyyy}. " +
            //                      $"Please renew to continue accessing the platform.",
            //            ClickUrl = $"/organizations/{@event.OrganizationId}/subscriptions",
            //        };

            //        await _mediator.Send(notificationCommand);

            //        // Send real-time notification via SignalR
            //        await _hubContext
            //            .Clients.User(adminUserId)
            //            .SendAsync("ReceiveNotification", notificationCommand);

            //        _logger.LogInformation(
            //            "SubscriptionExpiryWarningConsumer: Sent in-app notification to admin {AdminUserId}",
            //            adminUserId);
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError(ex,
            //            "SubscriptionExpiryWarningConsumer: Failed to send in-app notification to admin {AdminUserId}",
            //            adminUserId);
            //    }
            //}

            // Send email notifications
            foreach (var adminEmail in adminEmails)
            {
                if (string.IsNullOrWhiteSpace(adminEmail))
                    continue;

                try
                {
                    var subject = EmailTemplateHelper.GetSubscriptionExpirySubject(
                        @event.OrganizationName,
                        @event.DaysUntilExpiry);

                    var renewalLink = $"{_clientAppSettings.BaseUrl}/organizations/{@event.OrganizationId}/subscriptions/renew";

                    var htmlBody = EmailTemplateHelper.GetSubscriptionExpiryHtmlBody(
                        organizationName: @event.OrganizationName,
                        planName: @event.PlanName,
                        expiryDate: @event.ExpiryDate,
                        daysUntilExpiry: @event.DaysUntilExpiry,
                        maxStudentSeats: @event.MaxStudentSeats,
                        maxTeacherSeats: @event.MaxTeacherSeats,
                        renewalLink: renewalLink);

                    await _emailService.SendEmailAsync(adminEmail, subject, htmlBody, true);

                    _logger.LogInformation(
                        "SubscriptionExpiryWarningConsumer: Sent expiry warning email to {Email}",
                        adminEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SubscriptionExpiryWarningConsumer: Failed to send email to {Email}",
                        adminEmail);
                }
            }

            _logger.LogInformation(
                "SubscriptionExpiryWarningConsumer: Completed processing expiry warning for Organization {OrganizationId}",
                @event.OrganizationId);
        }
    }
}
