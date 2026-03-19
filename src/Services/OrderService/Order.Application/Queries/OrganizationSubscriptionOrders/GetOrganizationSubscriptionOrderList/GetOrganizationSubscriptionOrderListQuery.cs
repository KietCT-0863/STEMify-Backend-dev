using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderList
{
    public class GetOrganizationSubscriptionOrderListQuery : IRequest<GrpcPagedOrganizationSubscriptionOrderResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public int? OrganizationId { get; set; }
        public int? PlanBillingCycleId { get; set; }
        public int? ContractId { get; set; }
        public int? ParentSubscriptionId { get; set; }
        public Domain.Enums.OrganizationSubscriptionOrderStatus? Status { get; set; }
    }
}
