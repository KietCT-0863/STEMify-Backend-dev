using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class CourseLearningOutcomeRepository : EfRepositoryBase<ResourceDbContext, CourseLearningOutcome, int>, ICourseLearningOutcomeRepository
{
    public CourseLearningOutcomeRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
