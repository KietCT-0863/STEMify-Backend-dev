using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class AssignmentRepository
    : EfRepositoryBase<ResourceDbContext, Assignment, int>,
        IAssignmentRepository
{
    public AssignmentRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
