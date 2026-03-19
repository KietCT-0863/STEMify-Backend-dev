using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class PlanRepository
        : EfRepositoryBase<ProductDbContext, Plan, int>,
        IPlanRepository
    {
        public PlanRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
