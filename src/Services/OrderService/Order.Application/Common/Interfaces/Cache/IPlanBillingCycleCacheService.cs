using Shared.Protos.Product;

namespace Order.Application.Common.Interfaces.Cache
{
    public interface IPlanBillingCycleCacheService
    {
        Task<GrpcPlanBillingCycleModel> GetPlanBillingCycleByIdAsync(int id, CancellationToken cancellationToken);
    }
}
