using Identity.Application.Common.Interfaces;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data.Seeders;

public class OrganizationUserSeeder : IOrganizationUserSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationUserSeeder> _logger;

    public int Order => 5;

    public OrganizationUserSeeder(
        ApplicationDbContext context,
        ILogger<OrganizationUserSeeder> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedOrganizationUsersAsync(cancellationToken);
    }

    public async Task SeedOrganizationUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default OrganizationUsers...");

        var defaultOrgUsers = SeedDataConstants.DefaultOrganizationUsers.All;

        foreach (var orgUserData in defaultOrgUsers)
        {
            await SeedOrganizationUser(orgUserData, cancellationToken);
        }

        _logger.LogInformation("OrganizationUser seeding completed");
    }

    private async Task SeedOrganizationUser(
        OrganizationUserSeedData orgUserData,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userId = Guid.Parse(orgUserData.UserId);

            var existingOrgUser = await _context.OrganizationUsers
                .FirstOrDefaultAsync(
                    ou =>
                        ou.OrganizationId == orgUserData.OrganizationId
                        && ou.UserId == userId,
                    cancellationToken
                );

            if (existingOrgUser == null)
            {
                var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                if (user == null)
                {
                    return;
                }

                OrganizationUser orgUser;

                int? groupId = null;
                if (!string.IsNullOrEmpty(orgUserData.GroupName))
                {
                    var group = await _context.Groups
                        .FirstOrDefaultAsync(
                            g =>
                                g.OrganizationId == orgUserData.OrganizationId
                                && g.Name == orgUserData.GroupName,
                            cancellationToken
                        );

                    if (group != null)
                    {
                        groupId = group.Id;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Group {GroupName} not found for OrganizationId {OrganizationId}. Creating OrganizationUser without Group.",
                            orgUserData.GroupName,
                            orgUserData.OrganizationId
                        );
                    }
                }

                if (orgUserData.OrganizationRole == OrganizationRole.Teacher)
                {
                    orgUser = OrganizationUser.CreateTeacher(
                        organizationId: orgUserData.OrganizationId,
                        userId: userId,
                        subscriptionOrderId: orgUserData.SubscriptionOrderId,
                        specialization: orgUserData.TeacherSpecialization,
                        bio: orgUserData.Bio,
                        licenseAssignmentId: null
                    );
                }
                else if (orgUserData.OrganizationRole == OrganizationRole.Student)
                {
                    if (!orgUserData.StudentDateOfBirth.HasValue)
                    {
                        _logger.LogWarning(
                            "StudentDateOfBirth is required for Student. Skipping OrganizationUser creation for UserId: {UserId}",
                            userId
                        );
                        return;
                    }

                    orgUser = OrganizationUser.CreateStudent(
                        organizationId: orgUserData.OrganizationId,
                        userId: userId,
                        subscriptionOrderId: orgUserData.SubscriptionOrderId,
                        dateOfBirth: orgUserData.StudentDateOfBirth.Value,
                        major: orgUserData.StudentMajor,
                        bio: orgUserData.Bio,
                        licenseAssignmentId: null,
                        groupId: groupId
                    );
                }
                else
                {
                    orgUser = OrganizationUser.Create(
                        organizationId: orgUserData.OrganizationId,
                        userId: userId,
                        organizationRole: orgUserData.OrganizationRole,
                        licenseType: orgUserData.OrganizationRole.ToString(),
                        licenseAssignmentId: null,
                        subscriptionOrderId: orgUserData.SubscriptionOrderId,
                        groupId: groupId
                    );
                }

                _context.OrganizationUsers.Add(orgUser);
                await _context.SaveChangesAsync(cancellationToken);

            }
            else
            {
                _logger.LogInformation(
                    "OrganizationUser already exists (UserId: {UserId}, OrganizationId: {OrganizationId}), skipping",
                    userId,
                    orgUserData.OrganizationId
                );
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}

