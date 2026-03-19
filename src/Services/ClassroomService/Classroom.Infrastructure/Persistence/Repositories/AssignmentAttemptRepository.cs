using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class AssignmentAttemptRepository
        : EfRepositoryBase<ClassroomDbContext, AssignmentAttempt, int>,
            IAssignmentAttemptRepository
    {
        public AssignmentAttemptRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
