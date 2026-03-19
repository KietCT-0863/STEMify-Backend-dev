using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class AssignmentQuestionAttemptRepository
        : EfRepositoryBase<ClassroomDbContext, AssignmentQuestionAttempt, int>,
            IAssignmentQuestionAttemptRepository
    {
        public AssignmentQuestionAttemptRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
