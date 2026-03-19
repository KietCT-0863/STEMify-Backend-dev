using MediatR;
using Order.Application.Models;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetExpiringSubscriptions
{
    
    public class GetExpiringSubscriptionsQuery : IRequest<List<ExpiringSubscriptionDto>>
    {
        public int WarningDays { get; set; } = 30;

        public int? OrganizationId { get; set; }
    }
}
