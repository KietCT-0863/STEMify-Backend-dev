using Classroom.Application.Models.ClassroomModels;

namespace Classroom.Application.Common.Interfaces.Cache
{
    public interface ICurriculumCacheService
    {
        Task<CurriculumModel> GetByIdAsync(int id, CancellationToken cancellationToken);
    }
}
