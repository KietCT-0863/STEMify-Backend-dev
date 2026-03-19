using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class ComponentRepository
        : EfRepositoryBase<ProductDbContext, Component, int>,
        IComponentRepository
    {
        public ComponentRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
