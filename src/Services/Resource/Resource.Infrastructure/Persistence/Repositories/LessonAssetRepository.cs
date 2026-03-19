using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories
{
    public class LessonAssetRepository
        : EfRepositoryBase<ResourceDbContext, LessonAsset, int>,
        ILessonAssetRepository
    {
        public LessonAssetRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
