using Microsoft.Extensions.Logging;
using Product.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Product.Infrastructure.Services.Grpc
{
    public class GrpcCurriculumClient : IGrpcCurriculumClient
    {
        private readonly ILogger<GrpcCurriculumClient> _logger;
        private readonly CurriculumService.CurriculumServiceClient _client;

        public GrpcCurriculumClient(
            ILogger<GrpcCurriculumClient> logger,
            CurriculumService.CurriculumServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<CurriculumDetails> GetCurriculumByIdAsync(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get course by id: {id}", courseId);

            var request = new GetCurriculumRequest { Id = courseId };
            var response = await _client.GetCurriculumAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No curriculum found for id: {id}", courseId);
                throw new NotFoundException("No curriculum found");
            }

            return response;
        }
    }
}
