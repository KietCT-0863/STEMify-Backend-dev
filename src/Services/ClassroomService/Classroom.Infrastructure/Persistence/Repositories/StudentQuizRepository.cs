using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories
{
    public class StudentQuizRepository
        : EfRepositoryBase<ClassroomDbContext, StudentQuiz, int>,
            IStudentQuizRepository
    {
        public StudentQuizRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
