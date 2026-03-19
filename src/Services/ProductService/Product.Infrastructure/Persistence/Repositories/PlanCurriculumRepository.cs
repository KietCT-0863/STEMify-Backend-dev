using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class PlanCurriculumRepository
        : EfRepositoryBase<ProductDbContext, PlanCurriculum, int>,
        IPlanCurriculumRepository
    {
        public PlanCurriculumRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
