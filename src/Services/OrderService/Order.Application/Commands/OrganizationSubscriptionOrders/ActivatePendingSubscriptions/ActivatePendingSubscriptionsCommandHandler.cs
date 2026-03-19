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

namespace Order.Application.Commands.OrganizationSubscriptionOrders.ActivatePendingSubscriptions
{
    public class ActivatePendingSubscriptionsCommandHandler
        : IRequestHandler<ActivatePendingSubscriptionsCommand, int>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ActivatePendingSubscriptionsCommandHandler> _logger;

        public ActivatePendingSubscriptionsCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<ActivatePendingSubscriptionsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<int> Handle(
            ActivatePendingSubscriptionsCommand request,
            CancellationToken cancellationToken)
        {
            var executionTimeUtc = request.ExecutionTimeUtc ?? DateTime.UtcNow;
            var specification = new PendingOrganizationSubscriptionOrdersReadyForActivationSpecification(executionTimeUtc);

            var orders = await _unitOfWork.OrganizationSubscriptionOrders
                .GetAllAsync(specification, cancellationToken);

            if (!orders.Any())
            {
                _logger.LogInformation("ActivatePendingSubscriptions: no subscriptions ready for activation.");
                return 0;
            }

            var eventsToPublish = new List<SubscriptionActivatedEvent>();
            var activatedCount = 0;

            foreach (var order in orders)
            {
                if (order.Status == OrganizationSubscriptionOrderStatus.Active)
                {
                    continue;
                }

                order.Status = OrganizationSubscriptionOrderStatus.Active;
                order.LastModifiedDate = DateTimeOffset.UtcNow;

                await _unitOfWork.OrganizationSubscriptionOrders.UpdateAsync(order, cancellationToken);

                var @event = new SubscriptionActivatedEvent(
                    subscriptionOrderId: order.Id,
                    organizationId: order.OrganizationId,
                    organizationName: order.Organization?.Name ?? $"Organization {order.OrganizationId}",
                    planName: order.PlanName,
                    startDate: order.StartDate,
                    endDate: order.EndDate,
                    maxStudentSeats: order.MaxStudentSeats,
                    maxTeacherSeats: order.MaxTeacherSeats
                );

                eventsToPublish.Add(@event);
                activatedCount++;
            }

            if (activatedCount == 0)
            {
                _logger.LogInformation("ActivatePendingSubscriptions: matched subscriptions were already active.");
                return 0;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var @event in eventsToPublish)
            {
                await _publishEndpoint.Publish(@event, cancellationToken);
            }

            _logger.LogInformation("ActivatePendingSubscriptions: activated {Count} subscriptions.", activatedCount);
            return activatedCount;
        }
    }
}

