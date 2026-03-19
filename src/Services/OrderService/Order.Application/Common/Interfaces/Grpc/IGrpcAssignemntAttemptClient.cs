using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcAssignmentAttemptClient
    {
        Task<GrpcPagedAssignmentAttemptsResponse> GetPagedAssignmentAttempts(GetAssignmentAttemptParams request);
    }
}
