using Shared.Protos.User;

namespace Cart.Application.Common.Interfaces.Cache
{
    public interface IUserCacheService
    {
        Task<GrpcUserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
