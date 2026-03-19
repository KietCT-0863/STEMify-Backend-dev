using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class SectionProgressRepository
        : EfRepositoryBase<ClassroomDbContext, StudentSectionProgress, int>,
            ISectionProgressRepository
    {
        public SectionProgressRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
