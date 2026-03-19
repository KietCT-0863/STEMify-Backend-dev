using MediatR;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.DeleteOrganizationSubscriptionOrder
{
    public class DeleteOrganizationSubscriptionOrderCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }
}
