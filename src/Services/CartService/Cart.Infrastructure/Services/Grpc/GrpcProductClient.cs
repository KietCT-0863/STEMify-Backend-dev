using Cart.Application.Common.Interfaces.Grpc;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.Product;

namespace Cart.Infrastructure.Services.Grpc
{
    public class GrpcProductClient : IGrpcProductClient
    {
        private readonly ILogger<GrpcProductClient> _logger;
        private readonly ProductService.ProductServiceClient _client;

        public GrpcProductClient(
            ILogger<GrpcProductClient> logger,
            ProductService.ProductServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<ProductResponse> GetProductByIdAsync(int ProductId)
        {
            _logger.LogInformation("Calling GRPC Service to get Product by id: {id}", ProductId);

            var request = new GetProductRequest { Id = ProductId };
            var response = await _client.GetProductAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No Product found for id: {id}", ProductId);
                throw new NotFoundException("No Product found");
            }

            return response;
        }
    }
}
