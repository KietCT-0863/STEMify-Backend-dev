using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcQuizClient
    {
        Task<QuizResponse?> GetQuizByIdAsync(int id);
    }
}
