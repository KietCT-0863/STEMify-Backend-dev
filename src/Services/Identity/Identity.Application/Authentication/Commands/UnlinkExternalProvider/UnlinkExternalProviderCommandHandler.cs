using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Authentication.Commands.UnlinkExternalProvider;

/// <summary>
/// Handler for UnlinkExternalProviderCommand
/// </summary>
public class UnlinkExternalProviderCommandHandler
    : IRequestHandler<UnlinkExternalProviderCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkExternalProviderCommandHandler> _logger;

    public UnlinkExternalProviderCommandHandler(
        UserManager<ApplicationUser> userManager,
        IIdentityUnitOfWork unitOfWork,
        ILogger<UnlinkExternalProviderCommandHandler> logger
    )
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        UnlinkExternalProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        // Find the user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning(
                "User {UserId} not found when trying to unlink external provider",
                request.UserId
            );
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Get all logins for this user
        var logins = await _userManager.GetLoginsAsync(user);

        // Find the login to remove
        var loginToRemove = logins.FirstOrDefault(l => l.LoginProvider == request.ProviderName);

        if (loginToRemove == null)
        {
            _logger.LogWarning(
                "User {Email} does not have a login from provider {Provider}",
                user.Email,
                request.ProviderName
            );
            throw new InvalidOperationException(
                $"User does not have a login from provider {request.ProviderName}"
            );
        }

        // Check if user has a password or other logins
        var hasPassword = await _userManager.HasPasswordAsync(user);
        var otherLoginsCount = logins.Count(l => l.LoginProvider != request.ProviderName);

        if (!hasPassword && otherLoginsCount == 0)
        {
            _logger.LogWarning(
                "Cannot unlink the only external login for user {Email} who has no password",
                user.Email
            );
            throw new InvalidOperationException(
                "Cannot unlink the only login method. Please set a password first or link another external provider."
            );
        }

        // Remove the external login
        var result = await _userManager.RemoveLoginAsync(
            user,
            loginToRemove.LoginProvider,
            loginToRemove.ProviderKey
        );

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError(
                "Failed to unlink external login {Provider} from user {Email}: {Errors}",
                request.ProviderName,
                user.Email,
                errors
            );
            throw new InvalidOperationException($"Failed to unlink external login: {errors}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully unlinked external login {Provider} from user {Email}",
            request.ProviderName,
            user.Email
        );

        return true;
    }
}
