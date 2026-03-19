using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Commands.LicenseAssignments.ActivateReservedLicense;

public class ActivateReservedLicenseCommand : IRequest<ActivateReservedLicenseResponse>
{
    public int OrganizationId { get; set; }
    public string OrganizationUserId { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public int SubscriptionOrderId { get; set; }
}

