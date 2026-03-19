using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationById
{
    public class GetOrganizationByIdQuery : IRequest<GrpcOrganizationDetail>
    {
        public int Id { get; set; }
    }
}
