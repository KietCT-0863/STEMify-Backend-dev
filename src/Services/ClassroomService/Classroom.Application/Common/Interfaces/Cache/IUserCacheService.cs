using Classroom.Application.Models.EnrollmentModels;

namespace Classroom.Application.Common.Interfaces.Cache
{
    public interface IUserCacheService
    {
        Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<UserModel> GetOrganizationUserByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
