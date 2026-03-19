using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface IOrganizationRepository : IRepositoryBaseAsync<Domain.Entities.Organization, int> { }
}
