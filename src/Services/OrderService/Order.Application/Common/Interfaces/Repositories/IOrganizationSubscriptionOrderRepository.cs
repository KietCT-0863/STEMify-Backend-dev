using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface IOrganizationSubscriptionOrderRepository : IRepositoryBaseAsync<Domain.Entities.OrganizationSubscriptionOrder, int> { }
}
