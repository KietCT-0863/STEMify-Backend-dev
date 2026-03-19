using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Authentication.Commands.LinkExternalProvider;

/// <summary>
/// Handler for LinkExternalProviderCommand
/// </summary>
public class LinkExternalProviderCommandHandler : IRequestHandler<LinkExternalProviderCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<LinkExternalProviderCommandHandler> _logger;

    public LinkExternalProviderCommandHandler(
        UserManager<ApplicationUser> userManager,
        IIdentityUnitOfWork unitOfWork,
        ILogger<LinkExternalProviderCommandHandler> logger
    )
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        LinkExternalProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        var externalLoginInfo = request.ExternalLoginInfo;

        // Find the user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when trying to link external provider", request.UserId);
            throw new InvalidOperationException($"User with ID {request.UserId} not found");
        }

        // Check if this external login is already linked to another user
        var existingUser = await _userManager.FindByLoginAsync(
            externalLoginInfo.Provider,
            externalLoginInfo.ProviderKey
        );

        if (existingUser != null)
        {
            if (existingUser.Id == user.Id)
            {
                _logger.LogInformation(
                    "External login {Provider} is already linked to user {Email}",
                    externalLoginInfo.Provider,
                    user.Email
                );
                return true; // Already linked to this user
            }

            _logger.LogWarning(
                "External login {Provider} is already linked to another user",
                externalLoginInfo.Provider
            );
            throw new ExternalLoginAlreadyLinkedException(
                externalLoginInfo.Provider,
                externalLoginInfo.ProviderKey
            );
        }

        // Check if user already has a login from this provider
        var existingLogins = await _userManager.GetLoginsAsync(user);
        if (existingLogins.Any(l => l.LoginProvider == externalLoginInfo.Provider))
        {
            _logger.LogWarning(
                "User {Email} already has a login from {Provider}",
                user.Email,
                externalLoginInfo.Provider
            );
            throw new ExternalLoginAlreadyLinkedException(externalLoginInfo.Provider);
        }

        // Link the external login
        var result = await _userManager.AddLoginAsync(
            user,
            new UserLoginInfo(
                externalLoginInfo.Provider,
                externalLoginInfo.ProviderKey,
                externalLoginInfo.Provider
            )
        );

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError(
                "Failed to link external login {Provider} to user {Email}: {Errors}",
                externalLoginInfo.Provider,
                user.Email,
                errors
            );
            throw new ExternalAuthenticationFailedException(
                externalLoginInfo.Provider,
                $"Failed to link external login: {errors}"
            );
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully linked external login {Provider} to user {Email}",
            externalLoginInfo.Provider,
            user.Email
        );

        return true;
    }
}
