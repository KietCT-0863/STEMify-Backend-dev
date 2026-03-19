using Grpc.Core;
using MediatR;
using Product.Application.Features.PlanBillingCycles.Queries.GetPlanBillingCycleById;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class PlanBillingCycleGrpcService : GrpcPlanBillingCycleService.GrpcPlanBillingCycleServiceBase
    {
        private readonly IMediator _mediator;

        public PlanBillingCycleGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcPlanBillingCycleModel> GetPlanBillingCycleById(
            GetPlanBillingCycleRequest request,
            ServerCallContext context
        )
        {
            var query = new GetPlanBillingCycleByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}