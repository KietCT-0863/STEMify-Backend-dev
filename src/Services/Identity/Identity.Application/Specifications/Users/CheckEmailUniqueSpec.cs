using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to check if email is unique (excluding a specific user)
/// Replaces IUserRepositoryBase.IsEmailUniqueAsync method
/// </summary>
public class CheckEmailUniqueSpec : Specification<ApplicationUser>
{
    public CheckEmailUniqueSpec(string email, Guid? excludeUserId = null)
    {
        Query.Where(u => u.Email == email && u.Status != UserStatus.Deleted);

        if (excludeUserId.HasValue)
        {
            Query.Where(u => u.Id != excludeUserId.Value);
        }
    }
}
