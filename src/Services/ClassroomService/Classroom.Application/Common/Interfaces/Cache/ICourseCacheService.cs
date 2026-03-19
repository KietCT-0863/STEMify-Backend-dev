using Classroom.Application.Models.ClassroomModels;

namespace Classroom.Application.Common.Interfaces.Cache
{
    public interface ICourseCacheService
    {
        Task<CourseModel> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
