using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class SubscriptionOrderCurriculumRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.SubscriptionOrderCurriculum, int>,
        ISubscriptionOrderCurriculumRepository
    {
        public SubscriptionOrderCurriculumRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
