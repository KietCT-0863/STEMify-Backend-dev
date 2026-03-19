using Cart.Application.Common.Interfaces.Repositories;
using Contracts.Abstractions.Persistence.EfCore;

namespace Cart.Application.Common.Interfaces
{
    public interface ICartUnitOfWork : IEfUnitOfWork
    {
        ICartRepository Carts { get; }
    }
}
