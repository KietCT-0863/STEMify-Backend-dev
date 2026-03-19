using Classroom.Application.Common.Interfaces.Repositories;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories;

public class ClassroomRepository
    : EfRepositoryBase<ClassroomDbContext, Domain.Entities.Classroom, int>,
        IClassroomRepository
{
    public ClassroomRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
