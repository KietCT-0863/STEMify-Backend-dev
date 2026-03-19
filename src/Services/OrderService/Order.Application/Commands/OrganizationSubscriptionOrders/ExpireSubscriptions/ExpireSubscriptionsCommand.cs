using System;
using MediatR;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.ExpireSubscriptions
{
    public class ExpireSubscriptionsCommand : IRequest<int>
    {
        public DateTime? ExecutionTimeUtc { get; init; }
    }
}

