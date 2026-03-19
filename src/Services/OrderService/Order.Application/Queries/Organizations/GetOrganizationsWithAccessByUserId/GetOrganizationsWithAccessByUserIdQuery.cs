using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationsWithAccessByUserId
{
    public class GetOrganizationsWithAccessByUserIdQuery : IRequest<GrpcOrganizationsWithAccessResponse>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
