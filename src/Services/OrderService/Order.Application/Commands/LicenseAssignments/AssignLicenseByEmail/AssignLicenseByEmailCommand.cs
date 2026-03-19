using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Commands.LicenseAssignments.AssignLicenseByEmail;

public class AssignLicenseByEmailCommand : IRequest<AssignLicenseByEmailResponse>
{
    public int OrganizationId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public int SubscriptionOrderId { get; set; }
}
