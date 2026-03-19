using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to check if username is unique (excluding a specific user)
/// Replaces IUserRepositoryBase.IsUserNameUniqueAsync method
/// </summary>
public class CheckUserNameUniqueSpec : Specification<ApplicationUser>
{
    public CheckUserNameUniqueSpec(string userName, Guid? excludeUserId = null)
    {
        Query.Where(u => u.UserName == userName && u.Status != UserStatus.Deleted);

        if (excludeUserId.HasValue)
        {
            Query.Where(u => u.Id != excludeUserId.Value);
        }
    }
}
