using MediatR;
using Shared.Protos.Product;

namespace Product.Application.Features.PlanBillingCycles.Queries.GetPlanBillingCycleById
{
    public class GetPlanBillingCycleByIdQuery : IRequest<GrpcPlanBillingCycleModel>
    {
        public int Id { get; set; }
    }
}
