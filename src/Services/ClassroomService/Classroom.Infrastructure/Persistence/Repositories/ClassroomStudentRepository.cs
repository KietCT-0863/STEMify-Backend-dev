using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class ClassroomStudentRepository
    : EfRepositoryBase<ClassroomDbContext, ClassroomStudent, int>,
        IClassroomStudentRepository
    {
        public ClassroomStudentRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
