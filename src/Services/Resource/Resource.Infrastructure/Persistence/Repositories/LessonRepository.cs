using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class LessonRepository : EfRepositoryBase<ResourceDbContext, Lesson, int>, ILessonRepository
{
    public LessonRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
