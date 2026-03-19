using System;
using System.Collections.Generic;
using System.Linq;
using EventBus.Messages.Subscription;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Commands.OrganizationSubscriptionOrders.ActivatePendingSubscriptions;
using Order.Application.Commands.OrganizationSubscriptionOrders.ExpireSubscriptions;
using Order.Application.Models;
using Order.Application.Queries.OrganizationSubscriptionOrders.GetExpiringSubscriptions;

namespace Order.Application.Services
{
    public interface ISubscriptionLifecycleService
    {
        Task ActivatePendingSubscriptionsAsync(CancellationToken cancellationToken = default);

        Task ExpireEndedSubscriptionsAsync(CancellationToken cancellationToken = default);

        Task CheckAndNotifyExpiringSubscriptionsAsync(
            int warningDays = 30,
            CancellationToken cancellationToken = default);
    }

    public class SubscriptionLifecycleService : ISubscriptionLifecycleService
    {
        private readonly IMediator _mediator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IGrpcUserClient _grpcUserClient;
        private readonly ILogger<SubscriptionLifecycleService> _logger;

        public SubscriptionLifecycleService(
            IMediator mediator,
            IPublishEndpoint publishEndpoint,
            IGrpcUserClient grpcUserClient,
            ILogger<SubscriptionLifecycleService> logger)
        {
            _mediator = mediator;
            _publishEndpoint = publishEndpoint;
            _grpcUserClient = grpcUserClient;
            _logger = logger;
        }

        public async Task ActivatePendingSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var activatedCount = await _mediator.Send(
                    new ActivatePendingSubscriptionsCommand(),
                    cancellationToken);

                _logger.LogInformation(
                    "Subscription lifecycle: activated {Count} pending subscriptions",
                    activatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription lifecycle: error activating pending subscriptions");
                throw;
            }
        }

        public async Task ExpireEndedSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredCount = await _mediator.Send(
                    new ExpireSubscriptionsCommand(),
                    cancellationToken);

                _logger.LogInformation(
                    "Subscription lifecycle: expired {Count} subscriptions",
                    expiredCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription lifecycle: error expiring subscriptions");
                throw;
            }
        }

        public async Task CheckAndNotifyExpiringSubscriptionsAsync(
            int warningDays = 30,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "Starting subscription expiry check for subscriptions expiring within {WarningDays} days",
                    warningDays);

                var query = new GetExpiringSubscriptionsQuery
                {
                    WarningDays = warningDays
                };

                var expiringSubscriptions = await _mediator.Send(query, cancellationToken);

                if (!expiringSubscriptions.Any())
                {
                    _logger.LogInformation("No expiring subscriptions found");
                    return;
                }

                _logger.LogInformation(
                    "Found {Count} expiring subscriptions",
                    expiringSubscriptions.Count);

                var adminCache = new Dictionary<int, (List<string> UserIds, List<string> Emails)>();

                foreach (var subscription in expiringSubscriptions)
                {
                    try
                    {
                        var (adminUserIds, adminEmails) = await GetOrganizationAdminContactsAsync(
                            subscription.OrganizationId,
                            adminCache,
                            cancellationToken);

                        var @event = new SubscriptionExpiryWarningEvent(
                            subscriptionOrderId: subscription.SubscriptionOrderId,
                            organizationId: subscription.OrganizationId,
                            organizationName: subscription.OrganizationName,
                            planName: subscription.PlanName,
                            expiryDate: subscription.ExpiryDate,
                            daysUntilExpiry: subscription.DaysUntilExpiry,
                            adminUserIds: adminUserIds,
                            adminEmails: adminEmails,
                            maxStudentSeats: subscription.MaxStudentSeats,
                            maxTeacherSeats: subscription.MaxTeacherSeats
                        );

                        await _publishEndpoint.Publish(@event, cancellationToken);

                        _logger.LogInformation(
                            "Published subscription expiry warning event for Organization {OrganizationId} " +
                            "(Subscription {SubscriptionId}, expires in {Days} days on {ExpiryDate:yyyy-MM-dd})",
                            subscription.OrganizationId,
                            subscription.SubscriptionOrderId,
                            subscription.DaysUntilExpiry,
                            subscription.ExpiryDate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to publish expiry warning event for subscription {SubscriptionId}",
                            subscription.SubscriptionOrderId);
                    }
                }

                _logger.LogInformation(
                    "Subscription expiry check completed. Processed {Count} subscriptions",
                    expiringSubscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subscription expiry check");
                throw;
            }
        }

        private async Task<(List<string> UserIds, List<string> Emails)> GetOrganizationAdminContactsAsync(
            int organizationId,
            IDictionary<int, (List<string> UserIds, List<string> Emails)> cache,
            CancellationToken cancellationToken)
        {
            if (cache.TryGetValue(organizationId, out var cachedValue))
            {
                return cachedValue;
            }

            IReadOnlyList<OrganizationAdminInfo> admins;
            try
            {
                admins = await _grpcUserClient.GetOrganizationAdminsAsync(organizationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retrieve organization admins for organization {OrganizationId}",
                    organizationId);
                var fallback = (new List<string>(), new List<string>());
                cache[organizationId] = fallback;
                return fallback;
            }

            var userIds = admins
                .Select(admin => admin.UserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var emails = admins
                .Select(admin => admin.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = (userIds, emails);
            cache[organizationId] = result;
            return result;
        }
    }
}

