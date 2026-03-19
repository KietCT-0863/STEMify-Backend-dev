using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Admin.GetSystemAdminDashboard
{
    public class GetSystemAdminDashboardQuery : IRequest<GetSystemAdminDashboardResponse>
    {
        public string? Period { get; set; }
    }
}
