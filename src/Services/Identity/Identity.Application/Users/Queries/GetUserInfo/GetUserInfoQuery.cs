using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Users.Queries.GetUserInfo;

/// <summary>
/// Query to get user information by user ID
/// </summary>
public class GetUserInfoQuery : IRequest<UserInfoDto>
{
    public Guid UserId { get; init; }

    public GetUserInfoQuery(Guid userId)
    {
        UserId = userId;
    }
}
