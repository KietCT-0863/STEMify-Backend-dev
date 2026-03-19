using Shared.Protos.Resource;

namespace Order.Application.Common.Interfaces.Cache
{
    public interface ICurriculumCacheService
    {
        Task<CurriculumDetails> GetCurriculumByIdAsync(int id, CancellationToken cancellationToken);
    }
}
