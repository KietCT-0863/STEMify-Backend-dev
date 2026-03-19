using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcSectionClient : IGrpcSectionClient
    {
        private readonly ILogger<GrpcSectionClient> _logger;
        private readonly SectionService.SectionServiceClient _client;

        public GrpcSectionClient(
            ILogger<GrpcSectionClient> logger,
            SectionService.SectionServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<PagedSectionList> GetSectionsAsync(QuerySectionsRequest request)
        {
            _logger.LogInformation("Calling GetSectionsAsync with request: {Request}", request);

            var response = await _client.QuerySectionsAsync(request);

            _logger.LogInformation("Received response with {Count} sections", response.Items.Count);

            return response;
        }

        public async Task<SectionResponse> GetSectionByIdAsync(int id)
        {
            _logger.LogInformation("Calling GRPC Service to get section by id: {id}", id);

            var request = new GetSectionRequest { Id = id };
            var response = await _client.GetSectionAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No section found for id: {id}", id);
                throw new NotFoundException("No section found");
            }

            return response;
        }
    }
}
