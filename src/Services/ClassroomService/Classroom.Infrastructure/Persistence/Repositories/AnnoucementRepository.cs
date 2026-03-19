using Classroom.Application.Common.Interfaces.Repositories;
using Classroom.Domain.Entities;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Classroom.Infrastructure.Persistence.Repositories;

public class AnnoucementRepository
    : EfRepositoryBase<ClassroomDbContext, Annoucement, int>,
        IAnnoucementRepository
{
    public AnnoucementRepository(ClassroomDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
