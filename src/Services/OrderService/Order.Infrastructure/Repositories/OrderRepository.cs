using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class OrderRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.Order, int>,
        IOrderRepository
    {
        public OrderRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
