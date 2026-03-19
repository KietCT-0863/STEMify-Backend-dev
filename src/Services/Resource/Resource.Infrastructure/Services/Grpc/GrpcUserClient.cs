using Microsoft.Extensions.Logging;
using Resource.Application.Common.Interfaces.Grpc;
using Resource.Application.Models.User;
using Shared.Exceptions;
using Shared.Protos.User;

namespace Resource.Infrastructure.Services.Grpc
{
    public class GrpcUserClient : IGrpcUserClient
    {
        private readonly ILogger<GrpcUserClient> _logger;
        private readonly GrpcUser.GrpcUserClient _client;

        public GrpcUserClient(ILogger<GrpcUserClient> logger, GrpcUser.GrpcUserClient client)
        {
            _logger = logger;
            _client = client;
        }

        public async Task<UserModel> GetUserByIdAsync(Guid id)
        {
            _logger.LogInformation("Calling GRPC Service to get user by id: {id}", id);

            var request = new GetUserRequest { Id = id.ToString() };
            var response = await _client.GetUserByIdAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No user found for id: {id}", id);
                throw new NotFoundException("No user found");
            }

            var userModel = new UserModel
            {
                UserId = response.UserId,
                Name =
                    string.IsNullOrWhiteSpace(response.FirstName) && string.IsNullOrWhiteSpace(response.LastName)
                        ? response.Email
                        : $"{response.FirstName} {response.LastName}".Trim(),
                Email = response.Email,
                ImageUrl = response.ImageUrl,
                Role = response.UserRole,
            };

            return userModel;
        }
    }
}
