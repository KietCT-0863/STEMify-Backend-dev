using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcAssignmentAttemptClient : IGrpcAssignmentAttemptClient
    {
        private readonly ILogger<GrpcAssignmentAttemptClient> _logger;
        private readonly GrpcAssignmentAttempt.GrpcAssignmentAttemptClient _client;

        public GrpcAssignmentAttemptClient(ILogger<GrpcAssignmentAttemptClient> logger, GrpcAssignmentAttempt.GrpcAssignmentAttemptClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedAssignmentAttemptsResponse> GetPagedAssignmentAttempts(GetAssignmentAttemptParams request)
        {
            _logger.LogInformation("Getting AssignmentAttempt with request: {@request}", request);

            var response = await _client.GetPagedAssignmentAttemptsAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No AssignmentAttempt found for request: {@request}", request);
                throw new NotFoundException("No AssignmentAttempt found");
            }

            return response;
        }
    }
}
