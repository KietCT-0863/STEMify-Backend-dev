using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class OrganizationSubscriptionOrderRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.OrganizationSubscriptionOrder, int>,
        IOrganizationSubscriptionOrderRepository
    {
        public OrganizationSubscriptionOrderRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
