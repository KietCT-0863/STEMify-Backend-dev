using Shared.Protos.Resource;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCurriculumClient
    {
        Task<CurriculumDetails> GetCurriculumByIdAsync(int curriculumId);
        Task<CurriculumRelationsResponse> GetCurriculumRelations(int curriculumId);
    }
}
