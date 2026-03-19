using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// Specification to get recently active users (based on last login)
/// </summary>
public class GetRecentlyActiveUsersSpec : Specification<ApplicationUser>
{
    public GetRecentlyActiveUsersSpec(int days = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        Query
            .Where(u =>
                u.LastLoginAt.HasValue
                && u.LastLoginAt >= cutoffDate
                && u.Status == UserStatus.Active
            )
            .OrderByDescending(u => u.LastLoginAt);
    }
}
