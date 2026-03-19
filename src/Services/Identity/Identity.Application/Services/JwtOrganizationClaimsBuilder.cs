using System.Text.Json;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.ReadModels;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared.Enums;

namespace Identity.Application.Services;

public class JwtOrganizationClaimsBuilder
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly ILogger<JwtOrganizationClaimsBuilder> _logger;

    public JwtOrganizationClaimsBuilder(
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        ILogger<JwtOrganizationClaimsBuilder> logger)
    {
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
        _logger = logger;
    }

    public async Task<string> BuildOrganizationsClaimAsync(Guid userId)
    {
        try
        {

            var organizationUsers = await _organizationUserRepository.GetByUserIdAsync(
                userId,
                activeOnly: true
            );

            if (!organizationUsers.Any())
            {
                _logger.LogInformation(
                    "User {UserId} has no active organization memberships",
                    userId
                );
                return "[]";
            }
            var organizationGroups = new List<object>();

            foreach (var group in organizationUsers.GroupBy(ou => ou.OrganizationId))
            {
                var licenseProjections = await _licenseReadRepository.GetByOrganizationIdAsync(
                    group.Key,
                    CancellationToken.None);

                // Group by OrganizationUserId to aggregate all licenses per user
                var licensesByOrgUserId = licenseProjections
                    .GroupBy(p => p.OrganizationUserId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var orgClaim = BuildOrganizationClaim(group.Key, group.ToList(), licensesByOrgUserId);
                organizationGroups.Add(orgClaim);
            }

            var aspNetRoles = organizationUsers
                .Select(ou => ou.User?.Role.ToString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .DefaultIfEmpty("Member")
                .Select(role => role!)
                .ToArray();

            var claimPayload = new
            {
                role = aspNetRoles,
                organizations = organizationGroups
            };

            var json = JsonSerializer.Serialize(claimPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug(
                "Built organizations claim for user {UserId} with {OrgCount} organizations",
                userId,
                organizationGroups.Count
            );

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error building organizations claim for user {UserId}",
                userId
            );
            return "[]";
        }
    }

    /// <summary>
    /// Build a single organization claim object    
    /// </summary>
    private object BuildOrganizationClaim(
        int organizationId,
        List<OrganizationUser> subscriptions,
        IReadOnlyDictionary<Guid, List<OrganizationUserLicenseReadModel>> licensesByOrgUserId)
    {
        // Build active roles
        var activeRoles = subscriptions
            .Select(ou =>
            {
                var matchingLicense = licensesByOrgUserId.TryGetValue(ou.Id, out var licenses)
                    ? licenses.FirstOrDefault(l =>
                        l.Status == LicenseAssignmentStatus.Active &&
                        l.LicenseType.Equals(ou.OrganizationRole.ToString(), StringComparison.OrdinalIgnoreCase))
                    : null;
                return BuildRoleClaim(ou, matchingLicense);
            })
            .Where(role => role != null)
            .ToList();

        var expiredRoles = subscriptions
            .Select(ou =>
            {
                var matchingLicense = licensesByOrgUserId.TryGetValue(ou.Id, out var licenses)
                    ? licenses.FirstOrDefault(l =>
                        (l.Status == LicenseAssignmentStatus.Expired || l.Status == LicenseAssignmentStatus.Revoked) &&
                        l.LicenseType.Equals(ou.OrganizationRole.ToString(), StringComparison.OrdinalIgnoreCase))
                    : null;
                return BuildExpiredRoleClaim(ou, matchingLicense);
            })
            .Where(role => role != null)
            .ToList();

        var expiredOrganizationUserIds = subscriptions
            .Where(ou =>
            {
                if (!licensesByOrgUserId.TryGetValue(ou.Id, out var licenses))
                    return false;
                
                return licenses.Any(l =>
                    (l.Status == LicenseAssignmentStatus.Expired || l.Status == LicenseAssignmentStatus.Revoked) &&
                    l.LicenseType.Equals(ou.OrganizationRole.ToString(), StringComparison.OrdinalIgnoreCase));
            })
            .Select(ou => ou.Id)
            .ToList();

        return new
        {
            id = organizationId,
            organizationUserId = subscriptions.Where(ou => ou.OrganizationId == organizationId).Select(ou => ou.Id).ToList(),
            roles = activeRoles,
            expiredRoles = expiredRoles,
            expiredOrganizationUserIds = expiredOrganizationUserIds
        };
    }

    private static object? BuildRoleClaim(
        OrganizationUser orgUser,
        OrganizationUserLicenseReadModel? projection)
    {
        var subscriptionId = projection?.SubscriptionOrderId;

        if (subscriptionId == null)
        {
            return null;
        }

        return new
        {
            type = orgUser.OrganizationRole.ToString(),
            subscriptionId = subscriptionId.Value
        };
    }

    private static object? BuildExpiredRoleClaim(
        OrganizationUser orgUser,
        OrganizationUserLicenseReadModel? projection)
    {
        var subscriptionId = projection?.SubscriptionOrderId;

        if (subscriptionId == null)
        {
            return null;
        }

        return new
        {
            type = orgUser.OrganizationRole.ToString(),
            subscriptionId = subscriptionId.Value,
            status = projection?.Status.ToString() ?? "Unknown"
        };
    }

    public async Task<List<(int OrganizationId, int SubscriptionId)>> GetUserOrganizationSubscriptionsAsync(Guid userId)
    {
        var organizationUsers = await _organizationUserRepository.GetByUserIdAsync(
            userId,
            activeOnly: true
        );

        var results = new List<(int OrganizationId, int SubscriptionId)>();

        foreach (var orgUser in organizationUsers)
        {
            // Get all license assignments for this OrganizationUser
            var licenseAssignments = await _licenseReadRepository.GetByOrganizationUserIdAsync(
                orgUser.Id,
                CancellationToken.None);

            // Find matching active license for this OrganizationUser and role
            var matchingLicense = licenseAssignments.FirstOrDefault(l => 
                l.Status == LicenseAssignmentStatus.Active && 
                l.LicenseType.Equals(orgUser.OrganizationRole.ToString(), StringComparison.OrdinalIgnoreCase));

            if (matchingLicense != null)
            {
                results.Add((orgUser.OrganizationId, matchingLicense.SubscriptionOrderId));
            }
        }

        return results;
    }

}
