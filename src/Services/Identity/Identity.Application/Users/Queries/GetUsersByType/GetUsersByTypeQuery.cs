using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.GetUsersByType;

/// <summary>
/// Query to get users by type with pagination
/// </summary>
public class GetUsersByTypeQuery : IRequest<PagedResult<UserSummaryDto>>
{
    public string UserType { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
