using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.SearchUsers;

public class SearchUsersQuery : IRequest<PagedResult<UserSummaryDto>>
{
    public int OrganizationId { get; set; }
    public int? SubscriptionOrderId { get; set; }
    public string? LicenseType { get; set; }
    public string? Search { get; set; }
    public string? OrderBy { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


