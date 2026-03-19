using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Models;
using Shared.Exceptions;
using Shared.Protos.User;

namespace Order.Infrastructure.Services.Grpc
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

        public async Task<GrpcUserResponse> GetUserByIdAsync(Guid id)
        {
            _logger.LogInformation("Calling GRPC Service to get user by id: {id}", id);

            var request = new GetUserRequest { Id = id.ToString() };
            var response = await _client.GetUserByIdAsync(request);

            if (response == null)
            {
                _logger.LogWarning("No user found for id: {id}", id);
                throw new NotFoundException("No user found");
            }


            return response;
        }

        public async Task<CheckUserExistsResponse> CheckUserExists(List<string> emails)
        {
            if (emails == null || emails.Count == 0)
                throw new ArgumentException("Email list cannot be null or empty", nameof(emails));

            // Deduplicate emails
            var uniqueEmails = emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation(
                "Calling identity-api to check user existence for {Count} email(s): {Emails}",
                uniqueEmails.Count,
                string.Join(", ", uniqueEmails));

            try
            {
                var request = new CheckUserExistsRequest();
                request.Emails.AddRange(uniqueEmails);

                var response = await _client.CheckUserExistsAsync(request);

                if (response == null)
                {
                    _logger.LogError("Null response received from identity-api");
                    throw new InvalidOperationException("No response received from identity service");
                }

                _logger.LogInformation(
                    "Identity-api returned results for {Count} user(s)",
                    response.Results?.Count ?? 0);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling identity-api to check user existence");
                throw;
            }
        }

        public async Task<CheckUserExistsResponse> CheckUserIdExists(List<string> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                throw new ArgumentException("UserIds list cannot be null or empty", nameof(userIds));

            // Deduplicate emails
            var uniqueIds = userIds
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation(
                "Calling identity-api to check user existence for {Count} email(s): {Emails}",
                uniqueIds.Count,
                string.Join(", ", uniqueIds));

            try
            {
                var request = new CheckUserIdExistsRequest();
                request.UserIds.AddRange(uniqueIds);

                var response = await _client.CheckUserIdExistsAsync(request);

                if (response == null)
                {
                    _logger.LogError("Null response received from identity-api");
                    throw new InvalidOperationException("No response received from identity service");
                }

                _logger.LogInformation(
                    "Identity-api returned results for {Count} user(s)",
                    response.Results?.Count ?? 0);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling identity-api to check user existence");
                throw;
            }
        }

        public async Task<OrganizationUserInfo> GetOrganizationUserByIdAsync(
            Guid organizationUserId,
            CancellationToken cancellationToken = default)
        {
           try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PagedOrganizationUserList> GetOrganizationUsersByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var request = new GetOrganizationUsersByUserIdRequest
            {
                UserId = userId.ToString(),
                PageNumber = 1,
                PageSize = 100
            };

            var response = await _client.GetOrganizationUsersByUserIdAsync(
                request,
                cancellationToken: cancellationToken);

            return response;
        }

        public async Task<OrganizationUserModel> GetOrganizationUserAsync(
            Guid userId,
            int organizationId,
            CancellationToken cancellationToken = default)
        {
            var request = new GetOrganizationUserRequest
            {
                UserId = userId.ToString(),
                OrganizationId = organizationId
            };

            var response = await _client.GetOrganizationUserAsync(
                request,
                cancellationToken: cancellationToken);

            return response;
        }

        public async Task<IReadOnlyList<OrganizationAdminInfo>> GetOrganizationAdminsAsync(
            int organizationId,
            CancellationToken cancellationToken = default)
        {
            if (organizationId <= 0)
            {
                throw new ArgumentException("Organization id must be greater than zero.", nameof(organizationId));
            }

            try
            {
                var request = new GetOrganizationAdminsRequest { OrganizationId = organizationId };
                var response = await _client.GetOrganizationAdminsAsync(request, cancellationToken: cancellationToken);

                if (response?.Admins == null || response.Admins.Count == 0)
                {
                    return Array.Empty<OrganizationAdminInfo>();
                }

                var admins = response.Admins
                    .Where(admin => !string.IsNullOrWhiteSpace(admin.UserId) || !string.IsNullOrWhiteSpace(admin.Email))
                    .Select(admin => new OrganizationAdminInfo(
                        admin.UserId ?? string.Empty,
                        admin.Email ?? string.Empty,
                        admin.FullName ?? string.Empty))
                    .ToList();

                return admins;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error calling identity-api to fetch organization admins for organization {OrganizationId}",
                    organizationId);
                throw;
            }
        }
    }
}
