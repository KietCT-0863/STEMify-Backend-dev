using Identity.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CheckUserExists
{
    public class CheckUserIdExistsCommandHandler : IRequestHandler<CheckUserIdExistsCommand, CheckUserExistsResponse>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly ILogger<CheckUserIdExistsCommandHandler> _logger;

        public CheckUserIdExistsCommandHandler(
            IIdentityUnitOfWork unitOfWork,
            ILogger<CheckUserIdExistsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CheckUserExistsResponse> Handle(
            CheckUserIdExistsCommand request,
            CancellationToken cancellationToken)
        {
            var response = new CheckUserExistsResponse();

            if (request?.UserIds == null || request.UserIds.Count == 0)
            {
                _logger.LogWarning("Empty email list received");
                return response;
            }

            // Normalize + deduplicate UserIds
            var userIds = request.UserIds
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            _logger.LogInformation("Checking {Count} email(s) for user existence", userIds.Count);

            foreach (var id in userIds)
            {
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(Guid.Parse(id), cancellationToken);

                    if (user == null)
                    {
                        _logger.LogInformation("User not found with email: {Email}", id);
                    }
                    else
                    {
                        response.Results.Add(new CheckUserExistsResult
                        {
                            UserId = user.Id.ToString(),
                            Email = user.Email
                        });
                        _logger.LogInformation("User found - Email: {Email}, UserId: {UserId}", user.Email, user.Id);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking user existence for email: {Email}", id);
                }
            }

            return response;
        }
    }
}