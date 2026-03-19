using MediatR;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Queries.GetPlanById
{
    public class GetPlanByIdQuery : IRequest<GrpcPlanDetail>
    {
        public int Id { get; set; }
    }
}
