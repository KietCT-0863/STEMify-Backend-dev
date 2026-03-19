using Classroom.Application.Models.ClassroomModels;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCurriculumClient
    {
        Task<CurriculumModel> GetCurriculumByIdAsync(int courseId);
    }
}
