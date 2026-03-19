using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.CheckLicenseAvailability;

public class CheckLicenseAvailabilityQuery : IRequest<CheckLicenseAvailabilityResponse>
{
    public int OrganizationId { get; set; }
    public string LicenseType { get; set; } = string.Empty;
    public int RequestedCount { get; set; }
}
