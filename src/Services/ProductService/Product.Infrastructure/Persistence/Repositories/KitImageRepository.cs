using Infrastructure.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;
using Product.Domain.Entities;
using Sieve.Services;

namespace Product.Infrastructure.Persistence.Repositories
{
    public class KitImageRepository
        : EfRepositoryBase<ProductDbContext, KitImage, int>,
        IKitImageRepository
    {
        public KitImageRepository(ProductDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
