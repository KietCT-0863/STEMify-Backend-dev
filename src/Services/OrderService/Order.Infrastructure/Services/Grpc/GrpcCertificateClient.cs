using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcCertificateClient : IGrpcCertificateClient
    {
        private readonly ILogger<GrpcCertificateClient> _logger;
        private readonly GrpcCertificate.GrpcCertificateClient _client;

        public GrpcCertificateClient(ILogger<GrpcCertificateClient> logger, GrpcCertificate.GrpcCertificateClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<GrpcPagedCertificatesResponse> GetPagedCertificates(GetCertificatesRequest request)
        {
            _logger.LogInformation("Getting Certificate with request: {@request}", request);

            var response = await _client.GetPagedCertificatesAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No Certificate found for request: {@request}", request);
                throw new NotFoundException("No Certificate found");
            }

            return response;
        }
    }
}
