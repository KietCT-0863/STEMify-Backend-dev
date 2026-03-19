using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.ClassroomModels;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Classroom.Infrastructure.Services.Grpc
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

        public async Task<CourseModel> GetCourseByIdAsync(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get course by id: {id}", courseId);

            var request = new GetCourseRequest { Id = courseId };
            var response = await _client.GetCourseAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No course found for id: {id}", courseId);
                throw new NotFoundException("No course found");
            }

            var course = response.ToCourseModel();
            return course;
        }
    }
}
