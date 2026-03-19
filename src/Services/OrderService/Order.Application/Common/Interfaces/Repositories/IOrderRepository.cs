using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface IOrderRepository : IRepositoryBaseAsync<Domain.Entities.Order, int> { }
}
