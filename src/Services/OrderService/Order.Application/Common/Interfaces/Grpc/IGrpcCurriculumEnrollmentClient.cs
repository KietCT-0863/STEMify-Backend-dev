using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCurriculumEnrollmentClient
    {
        Task<GrpcPagedCurriculumEnrollmentsResponse> GetPagedCurriculumEnrollments(GetCurriculumEnrollmentsRequest request);
    }
}
