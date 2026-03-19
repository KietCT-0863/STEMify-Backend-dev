using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationTypes.GetOrganizationTypeById
{
    public class GetOrganizationTypeByIdQuery : IRequest<GrpcOrganizationTypeModel>
    {
        public int Id { get; set; }
    }
}
