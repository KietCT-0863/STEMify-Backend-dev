using Identity.Application.Common.Models.Auth;
using Infrastructure.Abstractions.Paging;

namespace Identity.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Response model for paginated users list
/// </summary>
public record GetAllUsersResponse(
    IEnumerable<UserInfoDto> Data,
    int PageNumber,
    int PageSize,
    int TotalCount
) : PageList<UserInfoDto>(Data.ToList(), PageNumber, PageSize, TotalCount);
