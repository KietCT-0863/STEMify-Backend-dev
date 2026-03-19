using Shared.Protos.User;

namespace Product.Application.Common.Interfaces.Grpc
{
    public interface IGrpcUserClient
    {
        Task<GrpcUserResponse> GetUserByIdAsync(Guid id);
    }
}
