using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface IContractRepository : IRepositoryBaseAsync<Domain.Entities.Contract, int> { }
}
