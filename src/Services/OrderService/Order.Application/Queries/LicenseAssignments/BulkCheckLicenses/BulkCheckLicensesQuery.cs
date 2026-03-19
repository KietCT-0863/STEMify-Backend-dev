using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.BulkCheckLicenses;

public class BulkCheckLicensesQuery : IRequest<BulkCheckLicensesResponse>
{
    public int OrganizationId { get; set; }
    public List<LicenseCheckRequest> LicenseRequests { get; set; } = new();
}
