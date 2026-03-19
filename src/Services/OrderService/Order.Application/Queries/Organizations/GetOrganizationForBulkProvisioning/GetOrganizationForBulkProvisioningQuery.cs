using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationForBulkProvisioning;

public class GetOrganizationForBulkProvisioningQuery : IRequest<GrpcOrganizationBulkProvisioningInfo>
{
    public int Id { get; set; }
}
