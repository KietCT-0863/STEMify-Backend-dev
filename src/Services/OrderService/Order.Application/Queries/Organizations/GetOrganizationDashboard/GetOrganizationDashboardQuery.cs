using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationDashboard
{
    public class GetOrganizationDashboardQuery : IRequest<GetOrganizationDashboardResponse>
    {
        public int Id { get; set; }
        public string? Period { get; set; }
    }
}
