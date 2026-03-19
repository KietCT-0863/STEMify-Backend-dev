using Infrastructure.Abstractions.Persistence.EfCore;
using Resource.Application.Common.Interfaces.Repositories;
using Resource.Domain.Entities;
using Sieve.Services;

namespace Resource.Infrastructure.Persistence.Repositories;

public class AssignmentQuestionRepository
    : EfRepositoryBase<ResourceDbContext, AssignmentQuestion, int>,
        IAssignmentQuestionRepository
{
    public AssignmentQuestionRepository(ResourceDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
}
