using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class PlanBillingCycleRepository
        : EfRepositoryBase<ProductDbContext, PlanBillingCycle, int>,
        IPlanBillingCycleRepository
    {
        public PlanBillingCycleRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
