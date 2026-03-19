using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.ReadModels;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Enums;

namespace Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;

public class GetOrganizationUsersByOrganizationIdQueryHandler
    : IRequestHandler<GetOrganizationUsersByOrganizationIdQuery, PagedResult<OrganizationUserGroupedDto>>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly ILogger<GetOrganizationUsersByOrganizationIdQueryHandler> _logger;

    public GetOrganizationUsersByOrganizationIdQueryHandler(
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        ILogger<GetOrganizationUsersByOrganizationIdQueryHandler> logger)
    {
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
        _logger = logger;
    }

    public async Task<PagedResult<OrganizationUserGroupedDto>> Handle(
        GetOrganizationUsersByOrganizationIdQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing GetOrganizationUsersByOrganizationId query. OrganizationId: {OrganizationId}, ActiveOnly: {ActiveOnly}, PageNumber: {PageNumber}, PageSize: {PageSize}, Role: {Role}, SubscriptionOrderId: {SubscriptionOrderId}",
            request.OrganizationId,
            request.ActiveOnly,
            request.PageNumber,
            request.PageSize,
            request.Role ?? "null",
            request.SubscriptionOrderId ?? "null");

        if (request.OrganizationId <= 0)
        {
            return new PagedResult<OrganizationUserGroupedDto>(
                Enumerable.Empty<OrganizationUserGroupedDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        try
        {
            // Fetch all distinct user ids first so filters can be applied before paging
            var (allUserIds, totalDistinctUsers) = await _organizationUserRepository.GetDistinctUserIdsByOrganizationAsync(
                request.OrganizationId,
                request.ActiveOnly,
                pageNumber: 1,
                pageSize: int.MaxValue,
                cancellationToken);

            _logger.LogInformation(
                "Retrieved {UserCount} distinct user IDs for organization {OrganizationId}. Total unique users: {TotalUniqueUsers}",
                allUserIds.Count,
                request.OrganizationId,
                totalDistinctUsers);

            if (allUserIds.Count == 0)
            {
                _logger.LogInformation(
                    "No users found for organization {OrganizationId} with ActiveOnly={ActiveOnly}. Returning empty result.",
                    request.OrganizationId,
                    request.ActiveOnly);
                return new PagedResult<OrganizationUserGroupedDto>(
                    Enumerable.Empty<OrganizationUserGroupedDto>(),
                    0,
                    request.PageNumber,
                    request.PageSize);
            }

            var organizationUsers = await _organizationUserRepository.GetSubscriptionsForUsersAsync(
                request.OrganizationId,
                allUserIds,
                request.ActiveOnly,
                cancellationToken);

            _logger.LogInformation(
                "Retrieved {OrgUserCount} organization users for {UserCount} users in organization {OrganizationId}",
                organizationUsers.Count,
                allUserIds.Count,
                request.OrganizationId);

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                if (Enum.TryParse<OrganizationRole>(request.Role, ignoreCase: true, out var roleFilter))
                {
                    var beforeCount = organizationUsers.Count;
                    organizationUsers = organizationUsers
                        .Where(ou => ou.OrganizationRole == roleFilter)
                        .ToList();
                    _logger.LogInformation(
                        "Filtered by role {Role}: {BeforeCount} -> {AfterCount} organization users",
                        request.Role,
                        beforeCount,
                        organizationUsers.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid role filter provided: {Role}. Role filter will be ignored.",
                        request.Role);
                }
            }

            var licenseProjections = await _licenseReadRepository.GetByOrganizationIdAsync(
                request.OrganizationId,
                cancellationToken);

            _logger.LogInformation(
                "Retrieved {LicenseCount} license projections for organization {OrganizationId}",
                licenseProjections.Count,
                request.OrganizationId);

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusValue = request.Status.Trim();
                if (Enum.TryParse<LicenseAssignmentStatus>(statusValue, ignoreCase: true, out var licenseStatusFilter))
                {
                    var beforeCount = organizationUsers.Count;

                    var orgUserIdsWithMatchingStatus = licenseProjections
                        .Where(l => l.Status == licenseStatusFilter)
                        .Select(l => l.OrganizationUserId)
                        .Distinct()
                        .ToHashSet();

                    organizationUsers = organizationUsers
                        .Where(ou => orgUserIdsWithMatchingStatus.Contains(ou.Id))
                        .ToList();

                    _logger.LogInformation(
                        "Filtered by LicenseAssignmentStatus {Status}: {BeforeCount} -> {AfterCount} organization users",
                        statusValue,
                        beforeCount,
                        organizationUsers.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid status filter provided: {Status}. Status filter will be ignored.",
                        request.Status);
                }
            }

            if (request.GroupId.HasValue)
            {
                var groupId = request.GroupId.Value;
                var beforeCount = organizationUsers.Count;
                organizationUsers = organizationUsers
                    .Where(ou => groupId == 0 ? ou.GroupId == null : ou.GroupId == groupId)
                    .ToList();

                _logger.LogInformation(
                    "Filtered by GroupId {GroupId}: {BeforeCount} -> {AfterCount} organization users",
                    groupId,
                    beforeCount,
                    organizationUsers.Count);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToLower();
                var beforeCount = organizationUsers.Count;

                organizationUsers = organizationUsers
                    .Where(ou => ou.User != null && (
                        (ou.User.Email != null && ou.User.Email.ToLower().Contains(term)) ||
                        (ou.User.UserName != null && ou.User.UserName.ToLower().Contains(term)) ||
                        (ou.User.FullName != null && ou.User.FullName.ToLower().Contains(term)) ||
                        (ou.User.FirstName != null && ou.User.FirstName.ToLower().Contains(term)) ||
                        (ou.User.LastName != null && ou.User.LastName.ToLower().Contains(term))))
                    .ToList();

                _logger.LogInformation(
                    "Filtered by search term '{Search}': {BeforeCount} -> {AfterCount} organization users",
                    request.Search,
                    beforeCount,
                    organizationUsers.Count);
            }

            if (!string.IsNullOrWhiteSpace(request.SubscriptionOrderId))
            {
                if (int.TryParse(request.SubscriptionOrderId, out var subscriptionOrderId))
                {
                    var beforeLicenseCount = licenseProjections.Count;
                    licenseProjections = licenseProjections
                        .Where(l => l.SubscriptionOrderId == subscriptionOrderId)
                        .ToList();

                    var matchingOrgUserIds = licenseProjections
                        .Select(l => l.OrganizationUserId)
                        .Distinct()
                        .ToHashSet();

                    var beforeOrgUserCount = organizationUsers.Count;
                    organizationUsers = organizationUsers
                        .Where(ou => matchingOrgUserIds.Contains(ou.Id))
                        .ToList();

                    _logger.LogInformation(
                        "Filtered by SubscriptionOrderId {SubscriptionOrderId}: Licenses {BeforeLicenseCount} -> {AfterLicenseCount}, OrganizationUsers {BeforeOrgUserCount} -> {AfterOrgUserCount}, Matching OrgUserIds: {MatchingCount}",
                        subscriptionOrderId,
                        beforeLicenseCount,
                        licenseProjections.Count,
                        beforeOrgUserCount,
                        organizationUsers.Count,
                        matchingOrgUserIds.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Invalid SubscriptionOrderId format: {SubscriptionOrderId}. Subscription filter will be ignored.",
                        request.SubscriptionOrderId);
                }
            }

            var licensesByOrgUserId = licenseProjections
                .GroupBy(p => p.OrganizationUserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation(
                "Grouped licenses by OrganizationUserId. Total groups: {GroupCount}",
                licensesByOrgUserId.Count);

            var orgUsersWithoutUser = organizationUsers.Count(ou => ou.User == null);
            if (orgUsersWithoutUser > 0)
            {
                _logger.LogWarning(
                    "Found {Count} organization users without associated User entity. These will be excluded from results.",
                    orgUsersWithoutUser);
            }

            var groupedByUser = organizationUsers
                .Where(ou => ou.User != null)
                .GroupBy(ou => ou.UserId)
                .Select(g =>
                {
                    var firstOrgUser = g.First();
                    var user = firstOrgUser.User!;
                    var fullName = !string.IsNullOrWhiteSpace(user.FullName)
                        ? user.FullName
                        : $"{user.FirstName} {user.LastName}".Trim();

                    return new OrganizationUserGroupedDto
                    {
                        UserId = g.Key,
                        Email = user.Email ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        FullName = fullName,
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        LastLoginAt = user.LastLoginAt,
                        Subscriptions = g.SelectMany(ou =>
                        {
                            if (!licensesByOrgUserId.TryGetValue(ou.Id, out var licenses) || licenses.Count == 0)
                            {
                                return new[]
                                {
                                    new SubscriptionInfoDto
                                    {
                                        OrganizationUserId = ou.Id,
                                        OrganizationId = ou.OrganizationId,
                                        OrganizationRole = ou.OrganizationRole.ToString(),
                                        LicenseType = ou.OrganizationRole.ToString(),
                                        LicenseAssignmentId = null,
                                        SubscriptionOrderId = null,
                                        IsActive = false,
                                        JoinedAt = ou.JoinedAt,
                                        GroupName = ou.Group?.Name,
                                        GroupCode = ou.Group?.Code,
                                        Bio = ou.Bio,
                                        StudentDateOfBirth = ou.StudentDateOfBirth,
                                        StudentMajor = ou.StudentMajor,
                                        TeacherSpecialization = ou.TeacherSpecialization
                                    }
                                };
                            }

                            return licenses.Select(license => new SubscriptionInfoDto
                            {
                                OrganizationUserId = ou.Id,
                                OrganizationId = ou.OrganizationId,
                                OrganizationRole = ou.OrganizationRole.ToString(),
                                LicenseType = license.LicenseType,
                                LicenseAssignmentId = license.LicenseAssignmentId.ToString(),
                                SubscriptionOrderId = license.SubscriptionOrderId,
                                IsActive = license.Status == LicenseAssignmentStatus.Active,
                                JoinedAt = ou.JoinedAt,
                                GroupName = ou.Group?.Name,
                                GroupCode = ou.Group?.Code,
                                Bio = ou.Bio,
                                StudentDateOfBirth = ou.StudentDateOfBirth,
                                StudentMajor = ou.StudentMajor,
                                TeacherSpecialization = ou.TeacherSpecialization
                            });
                        })
                        .OrderByDescending(s => s.JoinedAt)
                        .ThenByDescending(s => s.IsActive)
                        .ToList()
                    };
                })
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .ToList();

            var filteredTotalUsers = groupedByUser.Count;
            var pagedUsers = groupedByUser
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            _logger.LogInformation(
                "Successfully processed query for organization {OrganizationId}. Returning {ResultCount} users (grouped) after paging, FilteredTotalUsers: {FilteredTotalUsers}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.OrganizationId,
                pagedUsers.Count,
                filteredTotalUsers,
                request.PageNumber,
                request.PageSize);

            return new PagedResult<OrganizationUserGroupedDto>(
                pagedUsers,
                filteredTotalUsers,
                request.PageNumber,
                request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing GetOrganizationUsersByOrganizationId query for OrganizationId: {OrganizationId}, ActiveOnly: {ActiveOnly}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                request.OrganizationId,
                request.ActiveOnly,
                request.PageNumber,
                request.PageSize);
            throw;
        }
    }

}

