using Shared.Protos.User;

namespace Cart.Application.Common.Interfaces.Grpc
{
    public interface IGrpcUserClient
    {
        Task<GrpcUserResponse> GetUserByIdAsync(Guid id);
    }
}
