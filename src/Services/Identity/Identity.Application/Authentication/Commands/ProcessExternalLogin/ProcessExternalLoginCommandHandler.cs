using Common.Logging.Metrics;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Common.Models.Auth;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Authentication.Commands.ProcessExternalLogin;

/// <summary>
/// Handler for ProcessExternalLoginCommand
/// </summary>
public class ProcessExternalLoginCommandHandler
    : IRequestHandler<ProcessExternalLoginCommand, ExternalAuthenticationResultDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ProcessExternalLoginCommandHandler> _logger;

    public ProcessExternalLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ILogger<ProcessExternalLoginCommandHandler> logger
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ExternalAuthenticationResultDto> Handle(
        ProcessExternalLoginCommand request,
        CancellationToken cancellationToken
    )
    {
        var externalLoginInfo = request.ExternalLoginInfo;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var method = externalLoginInfo.Provider?.ToLower() ?? "unknown";

        try
        {
            // Step 1: Check if user already has this external login
            var provider = externalLoginInfo.Provider ?? throw new ArgumentNullException(nameof(externalLoginInfo.Provider));
            var existingUser = await _userManager.FindByLoginAsync(
                provider,
                externalLoginInfo.ProviderKey ?? throw new ArgumentNullException(nameof(externalLoginInfo.ProviderKey))
            );

            if (existingUser != null)
            {
                // User already exists with this external login - just sign in
                _logger.LogInformation(
                    "User {Email} found with existing external login from {Provider}",
                    existingUser.Email,
                    externalLoginInfo.Provider
                );

                stopwatch.Stop();
                IdentityMetrics.RecordLogin(method, success: true, stopwatch.Elapsed);

                return ExternalAuthenticationResultDto.Success(
                    existingUser.Id,
                    existingUser.Email ?? string.Empty,
                    existingUser.FullName,
                    isNewUser: false,
                    requiresProfileCompletion: false
                );
            }

            // Step 2: Check if user exists by email
            var userByEmail = await _userManager.FindByEmailAsync(externalLoginInfo.Email);

            if (userByEmail != null)
            {
                // User exists with this email - link the external login
                _logger.LogInformation(
                    "Linking external login {Provider} to existing user {Email}",
                    externalLoginInfo.Provider,
                    userByEmail.Email
                );

                var addLoginResult = await _userManager.AddLoginAsync(
                    userByEmail,
                    new UserLoginInfo(
                        externalLoginInfo.Provider,
                        externalLoginInfo.ProviderKey,
                        externalLoginInfo.Provider
                    )
                );

                if (!addLoginResult.Succeeded)
                {
                    var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to link external login for user {Email}: {Errors}",
                        userByEmail.Email,
                        errors
                    );

                    stopwatch.Stop();
                    IdentityMetrics.RecordLogin(method, success: false, stopwatch.Elapsed);

                    return ExternalAuthenticationResultDto.Failure(
                        $"Failed to link external login: {errors}",
                        "EXTERNAL_LOGIN_LINK_FAILED"
                    );
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                IdentityMetrics.RecordLogin(method, success: true, stopwatch.Elapsed);

                return ExternalAuthenticationResultDto.Success(
                    userByEmail.Id,
                    userByEmail.Email ?? string.Empty,
                    userByEmail.FullName,
                    isNewUser: false,
                    requiresProfileCompletion: false
                );
            }

            // Step 3: Create new user with external login
            // TEMPORARILY DISABLED: Do not create new users automatically
            // Only allow existing users to login via external provider
            _logger.LogWarning(
                "External login attempt for non-existing user {Email} from {Provider} - rejected (user creation disabled)",
                externalLoginInfo.Email,
                externalLoginInfo.Provider
            );

            stopwatch.Stop();
            IdentityMetrics.RecordLogin(method, success: false, stopwatch.Elapsed);

            return ExternalAuthenticationResultDto.Failure(
                "Tài khoản không tồn tại trong hệ thống. Vui lòng liên hệ quản trị viên để được tạo tài khoản.",
                "USER_NOT_FOUND"
            );

            /*
            _logger.LogInformation(
                "Creating new user from external login {Provider} for email {Email}",
                externalLoginInfo.Provider,
                externalLoginInfo.Email
            );

            var newUser = await CreateUserFromExternalLogin(
                externalLoginInfo,
                request.DefaultUserRole,
                cancellationToken
            );

            if (newUser == null)
            {
                stopwatch.Stop();
                IdentityMetrics.RecordLogin(method, success: false, stopwatch.Elapsed);
                return ExternalAuthenticationResultDto.Failure(
                    "Failed to create user from external login",
                    "USER_CREATION_FAILED"
                );
            }

            stopwatch.Stop();
            IdentityMetrics.RecordLogin(method, success: true, stopwatch.Elapsed);
            // Also record registration since this is a new user
            IdentityMetrics.RecordUserRegistration(newUser.Role.ToString());

            return ExternalAuthenticationResultDto.Success(
                newUser.Id,
                newUser.Email ?? string.Empty,
                newUser.FullName,
                isNewUser: true,
                requiresProfileCompletion: true
            );
            */
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            IdentityMetrics.RecordLogin(method, success: false, stopwatch.Elapsed);

            _logger.LogError(
                ex,
                "Error processing external login from {Provider} for email {Email}",
                externalLoginInfo.Provider,
                externalLoginInfo.Email
            );

            return ExternalAuthenticationResultDto.Failure(
                ex.Message,
                "EXTERNAL_LOGIN_ERROR"
            );
        }
    }

    /// <summary>
    /// Creates a new user from external login information
    /// </summary>
    private async Task<ApplicationUser?> CreateUserFromExternalLogin(
        ExternalLoginInfoDto externalLoginInfo,
        UserRole defaultRole,
        CancellationToken cancellationToken
    )
    {
        // Generate username from email
        var userName = externalLoginInfo.Email.Split('@')[0];

        // Ensure username is unique
        var baseUserName = userName;
        var counter = 1;
        while (await _userManager.FindByNameAsync(userName) != null)
        {
            userName = $"{baseUserName}{counter}";
            counter++;
        }

        // Per business requirement 2.2: Google Login only sets AspNetRole = Member
        // Organization roles are determined by OrganizationUser, not platform role
        // Always create User entity with Member role for external logins
        var newUser = User.Create(
            Guid.NewGuid(),
            externalLoginInfo.Email,
            userName,
            externalLoginInfo.FirstName ?? string.Empty,
            externalLoginInfo.LastName ?? string.Empty,
            UserRole.Member 
        );

        // Create user without password (external login)
        var createResult = await _userManager.CreateAsync(newUser);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create user: {Errors}", errors);
            return null;
        }

        // Add external login
        var addLoginResult = await _userManager.AddLoginAsync(
            newUser,
            new UserLoginInfo(
                externalLoginInfo.Provider,
                externalLoginInfo.ProviderKey,
                externalLoginInfo.Provider
            )
        );

        if (!addLoginResult.Succeeded)
        {
            var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to add external login: {Errors}", errors);

            // Rollback - delete user
            await _userManager.DeleteAsync(newUser);
            return null;
        }

        // Add user to role 
        var roleResult = await _userManager.AddToRoleAsync(newUser, UserRole.Member.ToString());
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to assign role: {Errors}", errors);
        }

        // Mark email as confirmed (trusted external provider)
        newUser.ConfirmEmail();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created new user {Email} from external login {Provider}",
            newUser.Email,
            externalLoginInfo.Provider
        );

        return newUser;
    }
}
