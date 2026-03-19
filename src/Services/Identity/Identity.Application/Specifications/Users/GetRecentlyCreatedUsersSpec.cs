using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get recently created users
/// </summary>
public class GetRecentlyCreatedUsersSpec : Specification<ApplicationUser>
{
    public GetRecentlyCreatedUsersSpec(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        Query
            .Where(u => u.CreatedAt >= cutoffDate && u.Status == UserStatus.Active)
            .OrderByDescending(u => u.CreatedAt);
    }
}
