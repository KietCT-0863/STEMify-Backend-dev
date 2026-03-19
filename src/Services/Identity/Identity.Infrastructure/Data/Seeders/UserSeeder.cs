using Identity.Application.Common.Interfaces;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data.Seeders;

/// <summary>
/// Infrastructure implementation for user seeding
/// Implements Application layer interface, uses Domain constants
/// </summary>
public class UserSeeder : IUserSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserSeeder> _logger;

    public int Order => 2; // Users must be seeded after roles

    public UserSeeder(UserManager<ApplicationUser> userManager, ILogger<UserSeeder> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        //await SeedDefaultUsersAsync(cancellationToken);
        await SeedDefaultUsersAsyncV2(cancellationToken);
    }

    //public async Task SeedAsync(CancellationToken cancellationToken = default)
    //{
    //    await SeedDefaultUsersAsync(cancellationToken);
    //}

    public async Task SeedDefaultUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default system users...");

        // Use domain constants instead of hardcoded values
        var defaultUsers = SeedDataConstants.DefaultUsers.All;

        foreach (var userData in defaultUsers)
        {
            if (!userData.IsValid())
            {
                _logger.LogWarning(
                    "Invalid user data for {DisplayName}, skipping",
                    userData.DisplayName
                );
                continue;
            }

            await SeedUser(userData, cancellationToken);
        }

        _logger.LogInformation(" User seeding completed");
    }

    private async Task SeedUser(UserSeedData userData, CancellationToken cancellationToken)
    {
        try
        {
            // Check if user exists with retry logic
            var existingUser = await FindUserWithRetry(userData.Email, cancellationToken);

            if (existingUser == null)
            {
                var user = CreateApplicationUser(userData);

                var result = await _userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    // Set EmailConfirmed to true for seed users
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    
                    await _userManager.AddToRoleAsync(user, userData.Role);
                    _logger.LogInformation(
                        " Created user: {Email} with role {Role}",
                        userData.Email,
                        userData.Role
                    );
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to create user {Email}: {Errors}",
                        userData.Email,
                        errors
                    );
                    throw new InvalidOperationException(
                        $"Failed to create user {userData.Email}: {errors}"
                    );
                }
            }
            else
            {
                
                if (!existingUser.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(existingUser);
                }
                _logger.LogInformation("User {Email} already exists, skipping", userData.Email);
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(
                ex,
                "Unexpected error seeding user {Email}: {Message}",
                userData.Email,
                ex.Message
            );
            throw;
        }
    }

    public async Task SeedDefaultUsersAsyncV2(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default system users...");

        var defaultUsers = SeedDataConstants.DefaultUsersV2.All;

        foreach (var userData in defaultUsers)
        {
            if (!userData.IsValid())
            {
                _logger.LogWarning(
                    "Invalid user data for {DisplayName}, skipping",
                    userData.DisplayName
                );
                continue;
            }

            await SeedUserV2(userData, cancellationToken);
        }

        _logger.LogInformation(" User seeding completed");
    }

    private async Task SeedUserV2(UserSeedDataV2 userData, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(userData.Id);
            
            var existingUserById = await _userManager.FindByIdAsync(userId.ToString());
            var existingUserByEmail = existingUserById ?? await FindUserWithRetry(userData.Email, cancellationToken);

            if (existingUserById == null && existingUserByEmail == null)
            {
                var user = CreateApplicationUserV2(userData);

                var result = await _userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    
                    await _userManager.AddToRoleAsync(user, userData.Role);
                    _logger.LogInformation(
                        "Created user: {Email} with role {Role}",
                        userData.Email,
                        userData.Role
                    );
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to create user {Email}: {Errors}",
                        userData.Email,
                        errors
                    );
                    throw new InvalidOperationException(
                        $"Failed to create user {userData.Email}: {errors}"
                    );
                }
            }
            else
            {
                var existingUser = existingUserById ?? existingUserByEmail;
                // Ensure EmailConfirmed is true for existing seed users
                if (!existingUser!.EmailConfirmed)
                {
                    existingUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(existingUser);
                    
                }
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(
                ex,
                "Unexpected error seeding user {Email}: {Message}",
                userData.Email,
                ex.Message
            );
            throw;
        }
    }

    private static ApplicationUser CreateApplicationUser(UserSeedData userData)
    {
        var id = Guid.NewGuid();
        var email = userData.Email;
        var userName = GenerateValidUserName(userData.Email, userData.Role);
        var requestedRole = ConvertRoleStringToEnum(userData.Role);
        var platformRole = NormalizePlatformRole(requestedRole);
        var (firstName, lastName) = GetDefaultNames(platformRole, userData.Role);

        return User.Create(id, email, userName, firstName, lastName, platformRole);
    }

    private static ApplicationUser CreateApplicationUserV2(UserSeedDataV2 userData)
    {
        var id = Guid.Parse(userData.Id);
        var email = userData.Email;
        var userName = GenerateValidUserName(userData.Email, userData.Role);
        var requestedRole = ConvertRoleStringToEnum(userData.Role);
        var platformRole = NormalizePlatformRole(requestedRole);
        var (firstName, lastName) = GetDefaultNames(platformRole, userData.Role);

        return User.Create(id, email, userName, firstName, lastName, platformRole);
    }

    /// <summary>
    /// Generate a valid username from email and role
    /// </summary>
    private static string GenerateValidUserName(string email, string role)
    {
        // Extract username part from email (before @)
        var emailPrefix = email.Split('@')[0];

        // Replace invalid characters with underscore
        var cleanPrefix = System.Text.RegularExpressions.Regex.Replace(
            emailPrefix,
            @"[^a-zA-Z0-9._-]",
            "_"
        );

        // Ensure minimum length and add role suffix
        var userName = $"{cleanPrefix}_{role.ToLower()}";

        // Ensure it doesn't start or end with dot
        userName = userName.Trim('.');

        // Ensure minimum length
        if (userName.Length < 3)
        {
            userName = $"user_{role.ToLower()}_{Guid.NewGuid().ToString("N")[..6]}";
        }

        // Ensure maximum length
        if (userName.Length > 50)
        {
            userName = userName[..47] + "001";
        }

        return userName;
    }

    /// <summary>
    /// Convert role string to UserRole enum
    /// </summary>
    private static UserRole ConvertRoleStringToEnum(string roleString)
    {
        if (Enum.TryParse<UserRole>(roleString, true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Unknown role: {roleString}", nameof(roleString));
    }

    private static UserRole NormalizePlatformRole(UserRole requestedRole)
    {
        return requestedRole switch
        {
            UserRole.Admin => UserRole.Admin,
            UserRole.Staff => UserRole.Staff,
            _ => UserRole.Member,
        };
    }

    private static (string FirstName, string LastName) GetDefaultNames(UserRole platformRole, string requestedRole)
    {
        return platformRole switch
        {
            UserRole.Admin => ("System", "Administrator"),
            UserRole.Staff => ("System", "Staff"),
            _ => ("Default", string.IsNullOrWhiteSpace(requestedRole) ? "Member" : requestedRole),
        };
    }

    

    private async Task<ApplicationUser?> FindUserWithRetry(
        string email,
        CancellationToken cancellationToken,
        int maxRetries = 5
    )
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "User existence check attempt {Attempt}/{MaxRetries} failed for {Email}: {Message}",
                    attempt,
                    maxRetries,
                    email,
                    ex.Message
                );

                if (attempt == maxRetries)
                {
                    _logger.LogError(
                        "Failed to check user existence for {Email} after {MaxRetries} attempts",
                        email,
                        maxRetries
                    );
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        return null; // This shouldn't be reached, but compiler requires it
    }
}
