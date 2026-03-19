using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface IOrganizationTypeRepository : IRepositoryBaseAsync<Domain.Entities.OrganizationType, int> { }
}
