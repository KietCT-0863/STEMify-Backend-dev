using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class OrganizationRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.Organization, int>,
        IOrganizationRepository
    {
        public OrganizationRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
