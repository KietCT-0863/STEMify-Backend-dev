using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcRubricCriterionClient : IGrpcRubricCriterionClient
    {
        private readonly ILogger<GrpcRubricCriterionClient> _logger;
        private readonly RubricCriterionService.RubricCriterionServiceClient _client;

        public GrpcRubricCriterionClient(
            ILogger<GrpcRubricCriterionClient> logger,
            RubricCriterionService.RubricCriterionServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<RubricCriterionResponse?> GetRubricCriterionByIdAsync(int id)
        {
            _logger.LogInformation("Calling GRPC Service to get quiz by id: {id}", id);

            var request = new GetRubricCriterionRequest { Id = id };
            var response = await _client.GetRubricCriterionAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No content found for sectionId: {id}", id);
                throw new KeyNotFoundException($"No content found for sectionId: {id}");
            }

            return response;
        }

        public async Task<PagedRubricCriterionList?> GetQueryRubricCriterions(QueryRubricCriterionsRequest request)
        {
            _logger.LogInformation("Calling GRPC Service to get rubric criterions with request: {@request}", request);

            var response = await _client.QueryRubricCriterionsAsync(request);

            return response;
        }
    }
}
