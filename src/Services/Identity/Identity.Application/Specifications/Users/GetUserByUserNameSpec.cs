using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get user by username
/// Replaces IUserRepositoryBase.GetByUserNameAsync method
/// </summary>
public class GetUserByUserNameSpec : Specification<ApplicationUser>
{
    public GetUserByUserNameSpec(string userName)
    {
        Query.Where(u => u.UserName == userName && u.Status != UserStatus.Deleted);
    }
}
