using Identity.Application.Common.Interfaces;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data.Seeders;

/// <summary>
/// Infrastructure implementation for Group seeding
/// Implements Application layer interface, uses Domain constants
/// </summary>
public class GroupSeeder : IGroupSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GroupSeeder> _logger;

    public int Order => 4; // Groups seeded after Users, Roles, and OAuth

    public GroupSeeder(ApplicationDbContext context, ILogger<GroupSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedGroupsAsync(cancellationToken);
    }

    public async Task SeedGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default Groups...");

        var defaultGroups = SeedDataConstants.DefaultGroups.All;

        foreach (var groupData in defaultGroups)
        {
            await SeedGroup(groupData, cancellationToken);
        }

        _logger.LogInformation("Group seeding completed");
    }

    private async Task SeedGroup(GroupSeedData groupData, CancellationToken cancellationToken)
    {
        try
        {
            var existingGroup = await _context.Groups
                .FirstOrDefaultAsync(
                    g => g.OrganizationId == groupData.OrganizationId && g.Name == groupData.Name,
                    cancellationToken
                );

            if (existingGroup == null)
            {
                var createdByUserId = Guid.Parse(groupData.CreatedByUserId);
                var group = Group.CreateWithCode(
                    organizationId: groupData.OrganizationId,
                    name: groupData.Name,
                    createdByUserId: createdByUserId,
                    organizationCode: null, // Seed data doesn't have organization code, will use organizationId as fallback
                    groupSegment: groupData.Code,
                    description: groupData.Description,
                    grade: groupData.Grade
                );
                _context.Groups.Add(group);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation(
                    "Group {Name} already exists (OrganizationId: {OrganizationId}), skipping",
                    groupData.Name,
                    groupData.OrganizationId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error seeding Group {Name}: {Message}",
                groupData.Name,
                ex.Message
            );
            throw;
        }
    }
}

