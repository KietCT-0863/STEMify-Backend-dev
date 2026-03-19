using Shared.Protos.Resource;

namespace Product.Application.Common.Interfaces.Cache
{
    public interface ICourseCacheService
    {
        Task<CourseDetail> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
