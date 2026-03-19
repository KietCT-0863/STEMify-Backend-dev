using Identity.Application.Common.Interfaces;
using Identity.Domain.Constants;
using Identity.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data.Seeders;

/// <summary>
/// Infrastructure implementation for role seeding
/// Implements Application layer interface, uses Domain constants
/// </summary>
public class RoleSeeder : IRoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<RoleSeeder> _logger;

    public int Order => 1;

    public RoleSeeder(RoleManager<ApplicationRole> roleManager, ILogger<RoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(cancellationToken);
    }

    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding system roles...");

        // Use domain constants instead of hardcoded values
        var roles = SeedDataConstants.DefaultRoles.All;

        foreach (var roleData in roles)
        {
            await SeedRole(roleData, cancellationToken);
        }

        _logger.LogInformation(" Role seeding completed");
    }

    private async Task SeedRole(string roleName, CancellationToken cancellationToken)
    {
        try
        {
            // Retry logic for role existence check
            bool roleExists = await CheckRoleExistsWithRetry(roleName, cancellationToken);

            if (!roleExists)
            {
                var role = new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper(),
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError(
                        "Failed to create role {RoleName}: {Errors}",
                        roleName,
                        errors
                    );
                    throw new InvalidOperationException(
                        $"Failed to create role {roleName}: {errors}"
                    );
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists, skipping", roleName);
            }
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(
                ex,
                "Unexpected error seeding role {RoleName}: {Message}",
                roleName,
                ex.Message
            );
            throw;
        }
    }

    private async Task<bool> CheckRoleExistsWithRetry(
        string roleName,
        CancellationToken cancellationToken,
        int maxRetries = 5
    )
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await _roleManager.RoleExistsAsync(roleName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "Role existence check attempt {Attempt}/{MaxRetries} failed for {RoleName}: {Message}",
                    attempt,
                    maxRetries,
                    roleName,
                    ex.Message
                );

                if (attempt == maxRetries)
                {
                    _logger.LogError(
                        "Failed to check role existence for {RoleName} after {MaxRetries} attempts",
                        roleName,
                        maxRetries
                    );
                    throw;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        return false; // This shouldn't be reached, but compiler requires it
    }
}
