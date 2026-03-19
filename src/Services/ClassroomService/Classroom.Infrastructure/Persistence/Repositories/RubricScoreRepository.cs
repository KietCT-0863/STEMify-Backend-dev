using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class RubricScoreRepository
        : EfRepositoryBase<ClassroomDbContext, RubricScore, int>,
            IRubricScoreRepository
    {
        public RubricScoreRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
