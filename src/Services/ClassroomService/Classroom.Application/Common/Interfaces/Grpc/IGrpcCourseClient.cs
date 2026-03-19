using Classroom.Application.Models.ClassroomModels;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCourseClient
    {
        Task<CourseModel> GetCourseByIdAsync(int courseId);
    }
}
