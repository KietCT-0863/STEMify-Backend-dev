using Classroom.Application.Models.EnrollmentModels;
using Shared.Protos.User;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcUserClient
    {
        Task<UserModel> GetUserByIdAsync(Guid id);
        Task<List<UserModel>> GetUsersByEmailsAsync(List<string> emails);
        Task<OrganizationUserInfo> GetOrganizationUserByIdAsync(Guid organizationUserId, CancellationToken cancellationToken = default);
    }
}
