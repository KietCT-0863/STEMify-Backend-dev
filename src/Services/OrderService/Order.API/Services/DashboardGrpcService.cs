using Grpc.Core;
using MediatR;
using Order.Application.Queries.Admin.GetSystemAdminDashboard;
using Shared.Protos.Order;

namespace Order.API.Services
{
    public class DashboardGrpcService : GrpcDashboardService.GrpcDashboardServiceBase
    {
        private readonly IMediator _mediator;

        public DashboardGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GetSystemAdminDashboardResponse> GetSystemAdminDashboard(
            GetSystemAdminDashboardRequest request,
            ServerCallContext context
        )
        {
            var query = new GetSystemAdminDashboardQuery
            {
                Period = request.Period,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}