using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcQuizAttemptClient : IGrpcQuizAttemptClient
    {
        private readonly ILogger<GrpcQuizAttemptClient> _logger;
        private readonly GrpcQuizAttempt.GrpcQuizAttemptClient _client;

        public GrpcQuizAttemptClient(ILogger<GrpcQuizAttemptClient> logger, GrpcQuizAttempt.GrpcQuizAttemptClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedQuizAttemptsResponse> GetPagedQuizAttempts(GetQuizAttemptParams request)
        {
            _logger.LogInformation("Getting QuizAttempt with request: {@request}", request);

            var response = await _client.GetPagedQuizAttemptsAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No QuizAttempt found for request: {@request}", request);
                throw new NotFoundException("No QuizAttempt found");
            }

            return response;
        }
    }
}
