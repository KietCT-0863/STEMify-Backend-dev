using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface ISubscriptionOrderCurriculumRepository : IRepositoryBaseAsync<Domain.Entities.SubscriptionOrderCurriculum, int> { }
}
