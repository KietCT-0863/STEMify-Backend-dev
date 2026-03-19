using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCourseEnrollmentClient
    {
        Task<GrpcPagedCourseEnrollmentsResponse> GetPagedCourseEnrollments(GetCourseEnrollmentsRequest request);
    }
}
