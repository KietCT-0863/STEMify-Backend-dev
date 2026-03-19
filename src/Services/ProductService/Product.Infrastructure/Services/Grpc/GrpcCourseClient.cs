using Microsoft.Extensions.Logging;
using Product.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Product.Infrastructure.Services.Grpc
{
    public class GrpcCourseClient : IGrpcCourseClient
    {
        private readonly ILogger<GrpcCourseClient> _logger;
        private readonly CourseService.CourseServiceClient _client;

        public GrpcCourseClient(
            ILogger<GrpcCourseClient> logger,
            CourseService.CourseServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<CourseDetail> GetCourseByIdAsync(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get course by id: {id}", courseId);

            var request = new GetCourseRequest { Id = courseId };
            var response = await _client.GetCourseAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No course found for id: {id}", courseId);
                throw new NotFoundException("No course found");
            }

            return response;
        }
    }
}
