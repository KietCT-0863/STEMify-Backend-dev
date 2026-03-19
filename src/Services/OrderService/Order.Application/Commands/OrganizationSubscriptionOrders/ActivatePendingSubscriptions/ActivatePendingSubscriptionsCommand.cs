using System;
using MediatR;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.ActivatePendingSubscriptions
{
    public class ActivatePendingSubscriptionsCommand : IRequest<int>
    {
        public DateTime? ExecutionTimeUtc { get; init; }
    }
}

