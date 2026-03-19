using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Models.EnrollmentModels;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Protos.User;

namespace Classroom.Infrastructure.Services.Grpc
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
                Name = $"{response.FirstName} {response.LastName}",
                Email = response.Email,
                ImageUrl = response.ImageUrl,
            };

            return userModel;
        }

        public async Task<List<UserModel>> GetUsersByEmailsAsync(List<string> emails)
        {
            _logger.LogInformation("Calling GRPC Service to get users by emails: {email}", emails);

            var request = new CheckUserExistsRequest();
            request.Emails.AddRange(emails);

            var response = await _client.CheckUserExistsAsync(request);

            var userModels = response.Results.Select(user => new UserModel
            {
                UserId = user.UserId,
                Email = user.Email,
            }).ToList();
            return userModels;
        }

        public async Task<OrganizationUserInfo> GetOrganizationUserByIdAsync(
            Guid organizationUserId,
            CancellationToken cancellationToken = default)
        {
            var request = new GetOrganizationUserByIdRequest
            {
                OrganizationUserId = organizationUserId.ToString()
            };

            var response = await _client.GetOrganizationUserByIdAsync(request, cancellationToken: cancellationToken);

            if (response == null)
            {
                throw new NotFoundException("Organization user not found");
            }

            return response;
        }
    }
}
