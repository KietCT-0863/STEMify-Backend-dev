using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class CourseEnrollmentRepository
        : EfRepositoryBase<ClassroomDbContext, CourseEnrollment, int>,
            ICourseEnrollmentRepository
    {
        public CourseEnrollmentRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
