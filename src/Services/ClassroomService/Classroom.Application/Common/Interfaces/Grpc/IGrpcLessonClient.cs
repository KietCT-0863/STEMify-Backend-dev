using Classroom.Application.Models.ClassroomModels;
using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcLessonClient
    {
        Task<LessonModel> GetLessonByIdAsync(int lessonId);
        Task<PagedLessonList> GetLessonsAsync(QueryLessonsRequest request);
    }
}
