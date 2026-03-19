using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcQuizClient : IGrpcQuizClient
    {
        private readonly ILogger<GrpcQuizClient> _logger;
        private readonly QuizService.QuizServiceClient _client;

        public GrpcQuizClient(
            ILogger<GrpcQuizClient> logger,
            QuizService.QuizServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<QuizResponse> GetQuizByIdAsync(int id)
        {
            _logger.LogInformation("Calling GRPC Service to get quiz by id: {id}", id);

            var request = new GetQuizRequest { Id = id };
            var response = await _client.GetQuizAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No content found for sectionId: {id}", id);
                throw new KeyNotFoundException($"No content found for sectionId: {id}");
            }

            return response;
        }
    }
}
