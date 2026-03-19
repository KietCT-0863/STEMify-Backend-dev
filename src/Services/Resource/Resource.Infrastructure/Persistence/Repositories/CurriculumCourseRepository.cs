using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories
{
    public class CurriculumCourseRepository : EfRepositoryBase<ResourceDbContext, CurriculumCourse, int>, ICurriculumCourseRepository
    {
        public CurriculumCourseRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
