using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcAssignmentClient : IGrpcAssignmentClient
    {
        private readonly ILogger<GrpcAssignmentClient> _logger;
        private readonly GrpcAssignmentService.GrpcAssignmentServiceClient _client;

        public GrpcAssignmentClient(
            ILogger<GrpcAssignmentClient> logger,
            GrpcAssignmentService.GrpcAssignmentServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcAssignmentModel?> GetAssignmentByIdAsync(int id)
        {
            _logger.LogInformation("Calling GRPC Service to get quiz by id: {id}", id);

            var request = new GetAssignmentRequest { Id = id };
            var response = await _client.GetAssignmentByIdAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No content found for sectionId: {id}", id);
                throw new KeyNotFoundException($"No content found for sectionId: {id}");
            }

            return response;
        }
    }
}
