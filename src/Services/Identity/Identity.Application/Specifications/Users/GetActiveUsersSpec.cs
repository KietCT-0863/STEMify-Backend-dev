using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get active users
/// Replaces IUserRepositoryBase.GetActiveUsersAsync method
/// </summary>
public class GetActiveUsersSpec : Specification<ApplicationUser>
{
    public GetActiveUsersSpec()
    {
        Query.Where(u => u.Status == UserStatus.Active);
    }
}
