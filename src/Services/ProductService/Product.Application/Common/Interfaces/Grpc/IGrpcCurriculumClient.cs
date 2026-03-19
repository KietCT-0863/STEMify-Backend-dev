
using Shared.Protos.Resource;

namespace Product.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCurriculumClient
    {
        Task<CurriculumDetails> GetCurriculumByIdAsync(int curriculumId);
    }
}
