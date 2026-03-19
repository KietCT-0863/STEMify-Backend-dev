using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Contracts.GetContractById
{
    public class GetContractByIdQuery : IRequest<GrpcContractDetail>
    {
        public int Id { get; set; }
    }
}
