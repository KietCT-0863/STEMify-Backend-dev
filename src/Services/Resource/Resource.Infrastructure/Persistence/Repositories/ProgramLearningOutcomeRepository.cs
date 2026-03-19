using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class ProgramLearningOutcomeRepository : EfRepositoryBase<ResourceDbContext, ProgramLearningOutcome, int>, IProgramLearningOutcomeRepository
{
    public ProgramLearningOutcomeRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
