using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.SearchUsersByLicenseType;

public class SearchUsersByLicenseTypeQueryHandler
    : IRequestHandler<SearchUsersByLicenseTypeQuery, PagedResult<UserSummaryDto>>
{
    public async Task<PagedResult<UserSummaryDto>> Handle(
        SearchUsersByLicenseTypeQuery request,
        CancellationToken cancellationToken)
    {
        // This query is deprecated; license-based search should be done via Order/License context.
        // For backward compatibility, delegate to the unified SearchUsersQuery handler.
        var unified = new SearchUsers.SearchUsersQuery
        {
            OrganizationId = 0,
            LicenseType = request.LicenseType,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        // Mediator is not available here; callers should use SearchUsersQuery instead.
        throw new NotSupportedException("SearchUsersByLicenseTypeQuery is deprecated. Use SearchUsersQuery with LicenseType filter instead.");
    }
}


