
using Shared.Protos.Resource;

namespace Product.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCourseClient
    {
        Task<CourseDetail> GetCourseByIdAsync(int courseId);
    }
}
