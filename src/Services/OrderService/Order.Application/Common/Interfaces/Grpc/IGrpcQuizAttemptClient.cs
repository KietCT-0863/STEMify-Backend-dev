using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcQuizAttemptClient
    {
        Task<GrpcPagedQuizAttemptsResponse> GetPagedQuizAttempts(GetQuizAttemptParams request);
    }
}
