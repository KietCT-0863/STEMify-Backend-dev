using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcCurriculumEnrollmentClient : IGrpcCurriculumEnrollmentClient
    {
        private readonly ILogger<GrpcCurriculumEnrollmentClient> _logger;
        private readonly GrpcCurriculumEnrollment.GrpcCurriculumEnrollmentClient _client;

        public GrpcCurriculumEnrollmentClient(ILogger<GrpcCurriculumEnrollmentClient> logger, GrpcCurriculumEnrollment.GrpcCurriculumEnrollmentClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedCurriculumEnrollmentsResponse> GetPagedCurriculumEnrollments(GetCurriculumEnrollmentsRequest request)
        {
            _logger.LogInformation("Getting CurriculumEnrollment with request: {@request}", request);

            var response = await _client.GetPagedCurriculumEnrollmentsAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No CurriculumEnrollment found for request: {@request}", request);
                throw new NotFoundException("No CurriculumEnrollment found");
            }

            return response;
        }
    }
}
