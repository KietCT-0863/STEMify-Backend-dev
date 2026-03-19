using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.SearchUsersByLicenseType;

public class SearchUsersByLicenseTypeQuery : IRequest<PagedResult<UserSummaryDto>>
{
    public int OrganizationId { get; set; }
    public string LicenseType { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


