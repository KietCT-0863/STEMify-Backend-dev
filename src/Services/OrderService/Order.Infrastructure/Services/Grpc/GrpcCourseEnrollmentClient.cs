using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcCourseEnrollmentClient : IGrpcCourseEnrollmentClient
    {
        private readonly ILogger<GrpcCourseEnrollmentClient> _logger;
        private readonly GrpcCourseEnrollment.GrpcCourseEnrollmentClient _client;

        public GrpcCourseEnrollmentClient(ILogger<GrpcCourseEnrollmentClient> logger, GrpcCourseEnrollment.GrpcCourseEnrollmentClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedCourseEnrollmentsResponse> GetPagedCourseEnrollments(GetCourseEnrollmentsRequest request)
        {
            _logger.LogInformation("Getting CourseEnrollment with request: {@request}", request);

            var response = await _client.GetPagedCourseEnrollmentsAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No CourseEnrollment found for request: {@request}", request);
                throw new NotFoundException("No CourseEnrollment found");
            }

            return response;
        }
    }
}
