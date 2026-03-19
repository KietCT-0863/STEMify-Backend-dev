using Classroom.Domain.Entities;
using Contracts.Abstractions.Persistence;

namespace Classroom.Application.Common.Interfaces.Repositories
{
    public interface ICourseEnrollmentRepository : IRepositoryBaseAsync<CourseEnrollment, int> { }
}
