using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class SectionRepository
    : EfRepositoryBase<ResourceDbContext, Section, int>,
        ISectionRepository
{
    public SectionRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
