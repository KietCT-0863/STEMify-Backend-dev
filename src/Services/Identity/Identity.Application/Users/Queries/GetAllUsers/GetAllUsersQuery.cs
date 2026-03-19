using MediatR;
using Shared.SeedWork;

namespace Identity.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Query to get all users with pagination
/// </summary>
public class GetAllUsersQuery : PagingRequestParam, IRequest<GetAllUsersResponse>
{
    // Optional filters for user profiles
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
}
