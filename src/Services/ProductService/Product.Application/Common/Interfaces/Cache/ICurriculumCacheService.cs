using Shared.Protos.Resource;

namespace Product.Application.Common.Interfaces.Cache
{
    public interface ICurriculumCacheService
    {
        Task<CurriculumDetails> GetCurriculumByIdAsync(int id, CancellationToken cancellationToken);
    }
}
