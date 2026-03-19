using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Product;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcPlanBillingCycleClient : IGrpcPlanBillingCycleClient
    {
        private readonly ILogger<GrpcPlanBillingCycleClient> _logger;
        private readonly GrpcPlanBillingCycleService.GrpcPlanBillingCycleServiceClient _client;

        public GrpcPlanBillingCycleClient(
            ILogger<GrpcPlanBillingCycleClient> logger,
            GrpcPlanBillingCycleService.GrpcPlanBillingCycleServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPlanBillingCycleModel> GetPlanBillingCycleByIdAsync(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get course by id: {id}", courseId);

            var request = new GetPlanBillingCycleRequest { Id = courseId };
            var response = await _client.GetPlanBillingCycleByIdAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No curriculum found for id: {id}", courseId);
                throw new NotFoundException("No curriculum found");
            }

            return response;
        }
    }
}
