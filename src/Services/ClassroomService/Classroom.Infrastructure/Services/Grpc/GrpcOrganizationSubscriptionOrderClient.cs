using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Order;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcOrganizationSubscriptionOrderClient : IGrpcOrganizationSubscriptionOrderClient
    {
        private readonly ILogger<GrpcOrganizationSubscriptionOrderClient> _logger;
        private readonly GrpcOrganizationSubscriptionOrderService.GrpcOrganizationSubscriptionOrderServiceClient _client;
        private readonly GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient _licenseClient;

        public GrpcOrganizationSubscriptionOrderClient(
            ILogger<GrpcOrganizationSubscriptionOrderClient> logger,
            GrpcOrganizationSubscriptionOrderService.GrpcOrganizationSubscriptionOrderServiceClient client,
            GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceClient licenseClient
        )
        {
            _logger = logger;
            _client = client;
            _licenseClient = licenseClient;
        }

        public async Task<GrpcLicenseAssignmentListModel> CreateLicenseAssignmentAssignmentsAsync(CreateLicenseAssignmentsRequest request)
        {
            _logger.LogInformation("Calling GRPC Service to create license assignments");
            var response = await _licenseClient.CreateLicenseAssignmentAsync(request);
            return response;
        }

        public async Task<GrpcOrganizationSubscriptionOrderDetail> GetOrganizationSubscriptionByIdAsync(int id)
        {
            _logger.LogInformation("Calling GRPC Service to get subscription by id: {id}", id);

            var request = new GetOrganizationSubscriptionOrderRequest { Id = id };
            var response = await _client.GetOrganizationSubscriptionOrderByIdAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No subscription found for id: {id}", id);
                throw new NotFoundException("No subscription found");
            }

            return response;
        }
    }
}
