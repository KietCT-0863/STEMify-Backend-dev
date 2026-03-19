using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get users by role
/// </summary>
public class GetUsersByRoleSpec : Specification<ApplicationUser>
{
    public GetUsersByRoleSpec(UserRole role)
    {
        Query.Where(u => u.Role == role && u.Status == UserStatus.Active);
    }
}
