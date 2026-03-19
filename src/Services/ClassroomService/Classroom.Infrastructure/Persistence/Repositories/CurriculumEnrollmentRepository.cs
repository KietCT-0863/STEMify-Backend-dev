using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class CurriculumEnrollmentRepository
        : EfRepositoryBase<ClassroomDbContext, CurriculumEnrollment, int>,
            ICurriculumEnrollmentRepository
    {
        public CurriculumEnrollmentRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
