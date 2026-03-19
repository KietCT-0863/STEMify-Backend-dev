using System;
using System.Collections.Generic;
using System.Linq;
using EventBus.Messages.Subscription;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Order.Domain.Enums;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.ExpireSubscriptions
{
    public class ExpireSubscriptionsCommandHandler
        : IRequestHandler<ExpireSubscriptionsCommand, int>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ExpireSubscriptionsCommandHandler> _logger;

        public ExpireSubscriptionsCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<ExpireSubscriptionsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<int> Handle(
            ExpireSubscriptionsCommand request,
            CancellationToken cancellationToken)
        {
            var executionTimeUtc = request.ExecutionTimeUtc ?? DateTime.UtcNow;
            var specification = new ActiveOrganizationSubscriptionOrdersReadyForExpirationSpecification(executionTimeUtc);

            var orders = await _unitOfWork.OrganizationSubscriptionOrders
                .GetAllAsync(specification, cancellationToken);

            if (!orders.Any())
            {
                _logger.LogInformation("ExpireSubscriptions: no subscriptions ready for expiration.");
                return 0;
            }

            var eventsToPublish = new List<SubscriptionExpiredEvent>();
            var expiredCount = 0;

            foreach (var order in orders)
            {
                if (order.Status == OrganizationSubscriptionOrderStatus.Expired)
                {
                    continue;
                }

                order.Status = OrganizationSubscriptionOrderStatus.Expired;
                order.LastModifiedDate = DateTimeOffset.UtcNow;

                await _unitOfWork.OrganizationSubscriptionOrders.UpdateAsync(order, cancellationToken);

                var @event = new SubscriptionExpiredEvent(
                    subscriptionOrderId: order.Id,
                    organizationId: order.OrganizationId,
                    organizationName: order.Organization?.Name ?? $"Organization {order.OrganizationId}",
                    planName: order.PlanName,
                    endDate: order.EndDate
                );

                eventsToPublish.Add(@event);
                expiredCount++;
            }

            if (expiredCount == 0)
            {
                _logger.LogInformation("ExpireSubscriptions: matched subscriptions were already expired.");
                return 0;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var @event in eventsToPublish)
            {
                await _publishEndpoint.Publish(@event, cancellationToken);
            }

            _logger.LogInformation("ExpireSubscriptions: expired {Count} subscriptions.", expiredCount);
            return expiredCount;
        }
    }
}

