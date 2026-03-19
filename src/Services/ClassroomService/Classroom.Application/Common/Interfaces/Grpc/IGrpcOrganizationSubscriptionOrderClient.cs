using Shared.Protos.Order;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcOrganizationSubscriptionOrderClient
    {
        Task<GrpcOrganizationSubscriptionOrderDetail> GetOrganizationSubscriptionByIdAsync(int id);
        Task<GrpcLicenseAssignmentListModel> CreateLicenseAssignmentAssignmentsAsync(CreateLicenseAssignmentsRequest request);
    }
}
