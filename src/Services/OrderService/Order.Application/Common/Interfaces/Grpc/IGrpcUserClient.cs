using Order.Application.Models;
using Shared.Protos.User;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcUserClient
    {
        Task<GrpcUserResponse> GetUserByIdAsync(Guid id);
        Task<CheckUserExistsResponse> CheckUserExists(List<string> email);
        Task<CheckUserExistsResponse> CheckUserIdExists(List<string> email);
        Task<OrganizationUserInfo> GetOrganizationUserByIdAsync(Guid organizationUserId, CancellationToken cancellationToken = default);
        Task<PagedOrganizationUserList> GetOrganizationUsersByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<OrganizationAdminInfo>> GetOrganizationAdminsAsync(
            int organizationId,
            CancellationToken cancellationToken = default);
        Task<OrganizationUserModel> GetOrganizationUserAsync(
            Guid userId,
            int organizationId,
            CancellationToken cancellationToken = default);
    }
}
