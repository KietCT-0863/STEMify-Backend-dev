using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class RubricCriterionRepository
    : EfRepositoryBase<ResourceDbContext, RubricCriterion, int>,
        IRubricCriterionRepository
{
    public RubricCriterionRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
