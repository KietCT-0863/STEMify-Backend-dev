using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class LessonProgressRepository
        : EfRepositoryBase<ClassroomDbContext, StudentLessonProgress, int>,
            ILessonProgressRepository
    {
        public LessonProgressRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
