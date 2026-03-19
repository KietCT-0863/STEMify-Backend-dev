using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Users.Queries.GetUserInfoFromClaims;

/// <summary>
/// Query to get user information from claims with scopes
/// </summary>
public class GetUserInfoFromClaimsQuery : IRequest<UserInfoResponseDto>
{
    public string SubjectId { get; init; } = string.Empty;
    public List<string> Scopes { get; init; } = new();
}
