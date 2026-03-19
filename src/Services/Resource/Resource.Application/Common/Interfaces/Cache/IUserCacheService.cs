using Resource.Application.Models.User;

namespace Resource.Application.Common.Interfaces.Cache
{
    public interface IUserCacheService
    {
        Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
