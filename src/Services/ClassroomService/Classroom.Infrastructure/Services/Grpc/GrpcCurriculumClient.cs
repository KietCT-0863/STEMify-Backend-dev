using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.ClassroomModels;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
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

        public async Task<CurriculumModel> GetCurriculumByIdAsync(int curriculumId)
        {
            _logger.LogInformation("Calling GRPC Service to get curriculum by id: {id}", curriculumId);

            var request = new GetCurriculumRequest { Id = curriculumId };
            var response = await _client.GetCurriculumAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No curriculum found for id: {id}", curriculumId);
                throw new NotFoundException("No curriculum found");
            }

            var curriculum = response.ToCurriculumModel();
            return curriculum;
        }
    }
}
