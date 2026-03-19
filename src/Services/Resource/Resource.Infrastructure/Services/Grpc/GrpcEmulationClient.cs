using Emulator.API.Protos;
using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;

namespace Resource.Infrastructure.Services.Grpc
{
    public class GrpcEmulationClient : IGrpcEmulationClient
    {
        private readonly ILogger<GrpcEmulationClient> _logger;
        private readonly EmulatorService.EmulatorServiceClient _client;

        public GrpcEmulationClient(ILogger<GrpcEmulationClient> logger, EmulatorService.EmulatorServiceClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<EmulationDetailResponse> GetEmulationByIdAsync(string id)
        {
            _logger.LogInformation("Calling GRPC Service to get user by id: {id}", id);

            var request = new GetEmulationRequest { EmulationId = id };
            var response = await _client.GetEmulationAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No Emulation found for id: {id}", id);
                throw new NotFoundException("No Emulation found");
            }

            return response;
        }
    }
}
