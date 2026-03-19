using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcClassroomClient : IGrpcClassroomClient
    {
        private readonly ILogger<GrpcClassroomClient> _logger;
        private readonly GrpcClassroom.GrpcClassroomClient _client;

        public GrpcClassroomClient(ILogger<GrpcClassroomClient> logger, GrpcClassroom.GrpcClassroomClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedClassroomsResponse> GetPagedClassrooms(GetClassroomsRequest request)
        {
            _logger.LogInformation("Getting classrooms with request: {@request}", request);

            var response = await _client.GetPagedClassroomsAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No classrooms found for request: {@request}", request);
                throw new NotFoundException("No classroom found");
            }

            return response;
        }

        public async Task<GrpcClassroomResponse> GetClassroomById(GetClassroomRequest request)
        {
            _logger.LogInformation("Getting classroom by ID with request: {@request}", request);
            var response = await _client.GetClassroomByIdAsync(request);
            if (response == null)
            {
                _logger.LogWarning("No classroom found for request: {@request}", request);
                throw new NotFoundException("No classroom found");
            }
            return response;
        }
    }
}
