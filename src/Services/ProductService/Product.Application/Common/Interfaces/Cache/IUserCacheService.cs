using Shared.Protos.User;

namespace Product.Application.Common.Interfaces.Cache
{
    public interface IUserCacheService
    {
        Task<GrpcUserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
