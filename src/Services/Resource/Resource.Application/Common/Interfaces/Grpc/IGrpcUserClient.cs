using Resource.Application.Models.User;

namespace Resource.Application.Common.Interfaces.Grpc
{
    public interface IGrpcUserClient
    {
        Task<UserModel> GetUserByIdAsync(Guid id);
    }
}
