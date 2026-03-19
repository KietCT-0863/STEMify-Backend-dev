using Identity.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CheckUserExists
{
    public class CheckUserExistsCommandHandler : IRequestHandler<CheckUserExistsCommand, CheckUserExistsResponse>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly ILogger<CheckUserExistsCommandHandler> _logger;

        public CheckUserExistsCommandHandler(
            IIdentityUnitOfWork unitOfWork,
            ILogger<CheckUserExistsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CheckUserExistsResponse> Handle(
            CheckUserExistsCommand request,
            CancellationToken cancellationToken)
        {
            var response = new CheckUserExistsResponse();

            if (request?.Email == null || request.Email.Count == 0)
            {
                _logger.LogWarning("Empty email list received");
                return response;
            }

            // Normalize + deduplicate emails
            var emails = request.Email
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            _logger.LogInformation("Checking {Count} email(s) for user existence", emails.Count);

            foreach (var email in emails)
            {
                try
                {
                    var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);

                    if (user == null)
                    {
                        _logger.LogInformation("User not found with email: {Email}", email);
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
                    _logger.LogError(ex, "Error checking user existence for email: {Email}", email);
                }
            }

            return response;
        }
    }
}