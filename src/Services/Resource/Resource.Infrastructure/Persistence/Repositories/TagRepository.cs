using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories
{
    public class TagRepository
    : EfRepositoryBase<ResourceDbContext, Tag, int>,
        ITagRepository
    {
        public TagRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
