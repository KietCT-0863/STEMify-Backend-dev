using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class KitProductRepository
        : EfRepositoryBase<ProductDbContext, KitProduct, int>,
        IKitProductRepository
    {
        public KitProductRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
