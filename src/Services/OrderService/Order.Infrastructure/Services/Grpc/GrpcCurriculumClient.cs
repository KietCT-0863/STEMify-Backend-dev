using Grpc.Core;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Order.Infrastructure.Services.Grpc
{
    public class GrpcCurriculumClient : IGrpcCurriculumClient
    {
        private readonly ILogger<GrpcCurriculumClient> _logger;
        private readonly CurriculumService.CurriculumServiceClient _client;

        public GrpcCurriculumClient(
            ILogger<GrpcCurriculumClient> logger,
            CurriculumService.CurriculumServiceClient client
        )
        {
            _logger = logger;
            _client = client;
        }

        public async Task<CurriculumDetails> GetCurriculumByIdAsync(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get curriculum by id: {id}", courseId);

            try
            {
                var request = new GetCurriculumRequest { Id = courseId };
                var response = await _client.GetCurriculumAsync(request);

                if (response == null)
                {
                    _logger.LogWarning("No curriculum found for id: {id}", courseId);
                    throw new NotFoundException($"No curriculum found for id: {courseId}");
                }

                _logger.LogInformation("Successfully retrieved curriculum for id: {id}", courseId);
                return response;
            }
            catch (RpcException rpcEx)
            {
                _logger.LogError(rpcEx, 
                    "gRPC error when getting curriculum for id: {id}. Status: {Status}, Detail: {Detail}", 
                    courseId, rpcEx.StatusCode, rpcEx.Status.Detail);
                
                // Re-throw with more context
                throw new Exception(
                    $"Failed to get curriculum for id {courseId}. gRPC Status: {rpcEx.StatusCode}, Detail: {rpcEx.Status.Detail}", 
                    rpcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error when getting curriculum for id: {id}", 
                    courseId);
                throw;
            }
        }

        public async Task<CurriculumRelationsResponse> GetCurriculumRelations(int courseId)
        {
            _logger.LogInformation("Calling GRPC Service to get curriculum relations for id: {id}", courseId);

            try
            {
                var request = new GetCurriculumRelationsRequest { Id = courseId };
                var response = await _client.GetCurriculumRelationsAsync(request);

                if (response == null)
                {
                   throw new NotFoundException($"No curriculum relations found for id: {courseId}");
                }

                _logger.LogInformation("Successfully retrieved curriculum relations for id: {id}", courseId);
                return response;
            }
            catch (RpcException rpcEx)
            {
               throw new Exception(
                    $"Failed to get curriculum relations for id {courseId}. gRPC Status: {rpcEx.StatusCode}, Detail: {rpcEx.Status.Detail}", 
                    rpcEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error when getting curriculum relations for id: {id}", 
                    courseId);
                throw;
            }
        }
    }
}
