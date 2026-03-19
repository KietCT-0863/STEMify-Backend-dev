using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using Identity.Domain.Entities;
using MediatR;
using Shared.Enums;

namespace Identity.Application.Users.Queries.SearchUsers;

public class SearchUsersQueryHandler : IRequestHandler<SearchUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;

    public SearchUsersQueryHandler(
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository)
    {
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
    }

    public async Task<PagedResult<UserSummaryDto>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        var page = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var size = request.PageSize <= 0 ? 20 : request.PageSize;

        List<OrganizationUser> baseQuery;
        if (request.SubscriptionOrderId.HasValue)
        {
            baseQuery = await _organizationUserRepository.GetBySubscriptionOrderIdAsync(
                request.SubscriptionOrderId.Value,
                activeOnly: true,
                cancellationToken);
        }
        else
        {
            baseQuery = await _organizationUserRepository.GetByOrganizationAsync(
                request.OrganizationId,
                activeOnly: true,
                pageNumber: 1,
                pageSize: 10000,
                cancellationToken);
        }


        IEnumerable<OrganizationUser> filtered = baseQuery;
        var licenseByOrgUserId = new Dictionary<Guid, ReadModels.OrganizationUserLicenseReadModel>();
        if (!string.IsNullOrWhiteSpace(request.LicenseType) && request.SubscriptionOrderId.HasValue)
        {
            var licenseProjections = await _licenseReadRepository.GetBySubscriptionOrderIdAsync(
                request.SubscriptionOrderId.Value,
                cancellationToken);

            var matchingLicenses = licenseProjections
                .Where(p => p.Status == LicenseAssignmentStatus.Active &&
                            string.Equals(p.LicenseType, request.LicenseType, StringComparison.OrdinalIgnoreCase))
                .ToList();
            licenseByOrgUserId = matchingLicenses
    .GroupBy(l => l.OrganizationUserId)
    .ToDictionary(g => g.Key, g => g.First());
            var orgUserIds = matchingLicenses
                .Select(p => p.OrganizationUserId)
                .Distinct()
                .ToHashSet();

            filtered = filtered.Where(ou => orgUserIds.Contains(ou.Id));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            filtered = filtered.Where(ou =>
                (ou.User.Email != null && ou.User.Email.ToLower().Contains(term)) ||
                (ou.User.UserName != null && ou.User.UserName.ToLower().Contains(term)) ||
                (ou.User.FullName != null && ou.User.FullName.ToLower().Contains(term)) ||
                (ou.User.FirstName != null && ou.User.FirstName.ToLower().Contains(term)) ||
                (ou.User.LastName != null && ou.User.LastName.ToLower().Contains(term)));
        }


        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        var items = filteredList
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        var userSummaries = items.Select(ou => 
        
        {
            licenseByOrgUserId.TryGetValue(ou.Id, out var license);
            return new UserSummaryDto
        {
             
            Id = ou.User.Id,
            Email = ou.User.Email ?? string.Empty,
            UserName = ou.User.UserName ?? string.Empty,
            FullName = ou.User.FullName ?? string.Empty,
            FirstName = ou.User.FirstName ?? string.Empty,
            LastName = ou.User.LastName ?? string.Empty,
            UserType = ou.User.Role.ToString(),
            Status = license?.Status.ToString() ?? ou.User.Status.ToString(),
            CreatedAt = ou.User.CreatedAt,
            LastLoginAt = ou.User.LastLoginAt,
        };
    });

        return new PagedResult<UserSummaryDto>(userSummaries, totalCount, page, size);
    }
}


