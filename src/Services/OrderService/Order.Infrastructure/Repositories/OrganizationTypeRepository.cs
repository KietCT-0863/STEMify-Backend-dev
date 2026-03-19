using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class OrganizationTypeRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.OrganizationType, int>,
        IOrganizationTypeRepository
    {
        public OrganizationTypeRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
