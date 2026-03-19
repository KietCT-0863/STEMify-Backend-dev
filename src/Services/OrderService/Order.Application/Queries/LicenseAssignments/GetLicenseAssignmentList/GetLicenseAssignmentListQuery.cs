using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentList
{
    public class GetLicenseAssignmentListQuery : IRequest<GrpcPagedLicenseAssignmentResponse>
    {
        public string? Search { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public string? OrderBy { get; set; }
        public bool IsDescending { get; set; }
        public int? OrganizationSubscriptionOrderId { get; set; }
        public string? UserId { get; set; }
        public Domain.Enums.LicenseAssignmentStatus? Status { get; set; }
        public Domain.Enums.LicenseType? Type { get; set; }
    }
}
