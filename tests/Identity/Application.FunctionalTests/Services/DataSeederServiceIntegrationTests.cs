using FluentAssertions;
using Identity.Application.Common.Interfaces;
using Identity.Application.FunctionalTests.Common;
using Identity.Application.FunctionalTests.Infrastructure;
using Identity.Domain.Constants;
using Identity.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Identity.Application.FunctionalTests.Services;

/// <summary>
/// Integration tests for DataSeederService
/// Testing the complete seeding workflow with real dependencies
/// </summary>
public class DataSeederServiceIntegrationTests : BaseIntegrationTest
{
    private readonly IDataSeeder _dataSeeder;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DataSeederServiceIntegrationTests(IdentityApplicationFactory factory)
        : base(factory)
    {
        _dataSeeder = GetRequiredService<IDataSeeder>();
        _userManager = GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = GetRequiredService<RoleManager<ApplicationRole>>();
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAllDefaultRoles()
    {
        // Act
        await _dataSeeder.SeedAsync();

        // Assert
        var expectedRoles = SeedDataConstants.DefaultRoles.All;

        foreach (var roleName in expectedRoles)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            roleExists.Should().BeTrue($"Role '{roleName}' should exist after seeding");
        }

        // Verify we have the expected number of roles
        var allRoles = await _roleManager.Roles.ToListAsync();
        allRoles.Should().HaveCount(expectedRoles.Count());
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateAllDefaultUsers()
    {
        // Act
        await _dataSeeder.SeedAsync();

        // Assert
        var defaultUsers = SeedDataConstants.DefaultUsers.All;

        foreach (var userData in defaultUsers)
        {
            var user = await _userManager.FindByEmailAsync(userData.Email);
            user.Should().NotBeNull($"User '{userData.Email}' should exist after seeding");

            if (user != null)
            {
                user.Email.Should().Be(userData.Email);
                user.EmailConfirmed.Should().BeTrue("Default users should have confirmed emails");

                // Verify user is in correct role
                var isInRole = await _userManager.IsInRoleAsync(user, userData.Role);
                isInRole
                    .Should()
                    .BeTrue($"User '{userData.Email}' should be in role '{userData.Role}'");
            }
        }
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_ShouldNotCreateDuplicates()
    {
        // Act - Run seeding twice
        await _dataSeeder.SeedAsync();
        await _dataSeeder.SeedAsync();

        // Assert - Should still have only the expected number of entities
        var expectedRoles = SeedDataConstants.DefaultRoles.All;
        var allRoles = await _roleManager.Roles.ToListAsync();
        allRoles.Should().HaveCount(expectedRoles.Count(), "Should not create duplicate roles");

        var expectedUsers = SeedDataConstants.DefaultUsers.All;
        var allUsers = await _userManager.Users.ToListAsync();
        allUsers.Should().HaveCount(expectedUsers.Count(), "Should not create duplicate users");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateRolesBeforeUsers()
    {
        // This test verifies the order dependency - roles must exist before users can be assigned to them

        // Act
        await _dataSeeder.SeedAsync();

        // Assert - If users exist with roles, then roles were created first
        var users = await _userManager.Users.ToListAsync();
        users.Should().NotBeEmpty("Users should be created");

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            userRoles.Should().NotBeEmpty($"User '{user.Email}' should have at least one role");

            foreach (var roleName in userRoles)
            {
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                roleExists
                    .Should()
                    .BeTrue($"Role '{roleName}' assigned to user '{user.Email}' should exist");
            }
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateUsersWithCorrectProperties()
    {
        // Act
        await _dataSeeder.SeedAsync();

        // Assert
        var adminUser = await _userManager.FindByEmailAsync("admin@stemify.com");
        adminUser.Should().NotBeNull();
        if (adminUser != null)
        {
            adminUser.UserName.Should().Be("admin@stemify.com");
            adminUser.EmailConfirmed.Should().BeTrue();
            adminUser.PhoneNumber.Should().NotBeNullOrEmpty();
            adminUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            var isAdmin = await _userManager.IsInRoleAsync(adminUser, "Admin");
            isAdmin.Should().BeTrue();
        }

        var teacherUser = await _userManager.FindByEmailAsync("teacher@stemify.com");
        teacherUser.Should().NotBeNull();
        if (teacherUser != null)
        {
            var isTeacher = await _userManager.IsInRoleAsync(teacherUser, "Teacher");
            isTeacher.Should().BeTrue();
        }

        var studentUser = await _userManager.FindByEmailAsync("student@stemify.com");
        studentUser.Should().NotBeNull();
        if (studentUser != null)
        {
            var isStudent = await _userManager.IsInRoleAsync(studentUser, "Student");
            isStudent.Should().BeTrue();
        }
    }

    [Fact]
    public async Task SeedAsync_WithExistingData_ShouldSkipExistingItems()
    {
        // Arrange - Create one role manually first
        var existingRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            NormalizedName = "ADMIN",
        };
        await _roleManager.CreateAsync(existingRole);

        // Act
        await _dataSeeder.SeedAsync();

        // Assert - Should have all expected roles (not duplicate the existing one)
        var allRoles = await _roleManager.Roles.ToListAsync();
        var expectedRoles = SeedDataConstants.DefaultRoles.All;
        allRoles.Should().HaveCount(expectedRoles.Count());

        // The existing role should still exist
        var adminRoles = allRoles.Where(r => r.Name == "Admin").ToList();
        adminRoles.Should().HaveCount(1, "Should not duplicate existing Admin role");
        adminRoles.First().Id.Should().Be(existingRole.Id, "Should preserve existing role");
    }

    [Fact]
    public async Task SeedSampleDataAsync_ShouldDelegateToSeedAsync()
    {
        // Act
        await _dataSeeder.SeedSampleDataAsync();

        // Assert - Should have same result as SeedAsync
        var expectedRoles = SeedDataConstants.DefaultRoles.All;
        var allRoles = await _roleManager.Roles.ToListAsync();
        allRoles.Should().HaveCount(expectedRoles.Count());

        var expectedUsers = SeedDataConstants.DefaultUsers.All;
        var allUsers = await _userManager.Users.ToListAsync();
        allUsers.Should().HaveCount(expectedUsers.Count());
    }

    [Fact]
    public async Task SeedAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _dataSeeder.SeedAsync(cts.Token)
        );
    }
}
