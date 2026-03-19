using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get user by email
/// Replaces IUserRepositoryBase.GetByEmailAsync method
/// </summary>
public class GetUserByEmailSpec : Specification<ApplicationUser>
{
    public GetUserByEmailSpec(string email)
    {
        Query.Where(u => u.Email == email && u.Status != UserStatus.Deleted);
    }
}
