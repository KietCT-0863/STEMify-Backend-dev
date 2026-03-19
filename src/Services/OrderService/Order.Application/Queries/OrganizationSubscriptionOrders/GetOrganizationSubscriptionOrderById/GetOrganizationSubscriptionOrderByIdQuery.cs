using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderById
{
    public class GetOrganizationSubscriptionOrderByIdQuery : IRequest<GrpcOrganizationSubscriptionOrderDetail>
    {
        public int Id { get; set; }
    }
}
