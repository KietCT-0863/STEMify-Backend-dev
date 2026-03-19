using Classroom.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
{
    public class GrpcContentClient : IGrpcContentClient
    {
        private readonly ILogger<GrpcContentClient> _logger;
        private readonly ContentService.ContentServiceClient _client;

        public GrpcContentClient(
            ILogger<GrpcContentClient> logger,
            ContentService.ContentServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<ContentResponse?> GetContentBySectionIdAsync(int sectionId)
        {
            _logger.LogInformation("Calling GRPC Service to get content by sectionid: {id}", sectionId);

            var request = new QueryContentsRequest { SectionId = sectionId };
            var response = await _client.QueryContentsAsync(request);

            if (response == null || response.Items.Count() == 0)
            {
                _logger.LogWarning("No content found for sectionId: {id}", sectionId);
                return null;
            }

            var content = response.Items.OrderByDescending(c => c.Id).FirstOrDefault();
            return content;
        }
    }
}
