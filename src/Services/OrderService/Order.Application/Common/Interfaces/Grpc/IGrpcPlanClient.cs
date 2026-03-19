using Shared.Protos.Product;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcPlanBillingCycleClient
    {
        Task<GrpcPlanBillingCycleModel> GetPlanBillingCycleByIdAsync(int courseId);
    }
}
