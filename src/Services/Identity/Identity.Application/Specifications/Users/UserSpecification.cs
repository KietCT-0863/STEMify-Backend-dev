using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Specifications.Users;

/// <summary>
/// General-purpose User specification with filtering, sorting, and paging
/// Follows Classroom service pattern for comprehensive query support
/// </summary>
public class UserSpecification : Specification<ApplicationUser>
{
    public UserSpecification(UserParams userParams)
    {
        // Apply filters
        if (!string.IsNullOrEmpty(userParams.Search))
        {
            Query.Where(u =>
                u.FirstName.Contains(userParams.Search)
                || u.LastName.Contains(userParams.Search)
                || u.Email!.Contains(userParams.Search)
                || u.UserName!.Contains(userParams.Search)
            );
        }

        if (!string.IsNullOrEmpty(userParams.Email))
        {
            Query.Where(u => u.Email == userParams.Email);
        }

        if (!string.IsNullOrEmpty(userParams.UserName))
        {
            Query.Where(u => u.UserName == userParams.UserName);
        }

        if (userParams.Role.HasValue)
        {
            Query.Where(u => u.Role == userParams.Role.Value);
        }

        if (userParams.Status.HasValue)
        {
            Query.Where(u => u.Status == userParams.Status.Value);
        }
        else
        {
            // Default to exclude deleted users
            Query.Where(u => u.Status != UserStatus.Deleted);
        }

        if (userParams.IsEmailConfirmed.HasValue)
        {
            if (userParams.IsEmailConfirmed.Value)
                Query.Where(u => u.EmailConfirmedAt.HasValue);
            else
                Query.Where(u => !u.EmailConfirmedAt.HasValue);
        }

        if (userParams.CreatedAfter.HasValue)
        {
            Query.Where(u => u.CreatedAt >= userParams.CreatedAfter.Value);
        }

        if (userParams.CreatedBefore.HasValue)
        {
            Query.Where(u => u.CreatedAt <= userParams.CreatedBefore.Value);
        }

        if (userParams.LastLoginAfter.HasValue)
        {
            Query.Where(u =>
                u.LastLoginAt.HasValue && u.LastLoginAt >= userParams.LastLoginAfter.Value
            );
        }

        if (userParams.LastLoginBefore.HasValue)
        {
            Query.Where(u =>
                u.LastLoginAt.HasValue && u.LastLoginAt <= userParams.LastLoginBefore.Value
            );
        }

        // Apply sorting
        if (!string.IsNullOrEmpty(userParams.SortOrder))
        {
            switch (userParams.SortOrder.ToLower())
            {
                case "firstnameasc":
                    Query.OrderBy(u => u.FirstName);
                    break;
                case "firstnamedesc":
                    Query.OrderByDescending(u => u.FirstName);
                    break;
                case "lastnameasc":
                    Query.OrderBy(u => u.LastName);
                    break;
                case "lastnamedesc":
                    Query.OrderByDescending(u => u.LastName);
                    break;
                case "emailasc":
                    Query.OrderBy(u => u.Email);
                    break;
                case "emaildesc":
                    Query.OrderByDescending(u => u.Email);
                    break;
                case "usernameasc":
                    Query.OrderBy(u => u.UserName);
                    break;
                case "usernamedesc":
                    Query.OrderByDescending(u => u.UserName);
                    break;
                case "roleasc":
                    Query.OrderBy(u => u.Role);
                    break;
                case "roledesc":
                    Query.OrderByDescending(u => u.Role);
                    break;
                case "statusasc":
                    Query.OrderBy(u => u.Status);
                    break;
                case "statusdesc":
                    Query.OrderByDescending(u => u.Status);
                    break;
                case "createddateasc":
                    Query.OrderBy(u => u.CreatedAt);
                    break;
                case "createddatedesc":
                    Query.OrderByDescending(u => u.CreatedAt);
                    break;
                case "lastloginasc":
                    Query.OrderBy(u => u.LastLoginAt);
                    break;
                case "lastlogindesc":
                    Query.OrderByDescending(u => u.LastLoginAt);
                    break;
                default:
                    Query.OrderByDescending(u => u.CreatedAt);
                    break;
            }
        }
        else
        {
            Query.OrderByDescending(u => u.CreatedAt);
        }

        // Apply pagination
        Query.Skip((userParams.PageNumber - 1) * userParams.PageSize).Take(userParams.PageSize);
    }
}

/// <summary>
/// Specification to get a user by ID
/// </summary>
public class UserByIdSpec : Specification<ApplicationUser>
{
    public UserByIdSpec(Guid id)
    {
        Query.Where(u => u.Id == id && u.Status != UserStatus.Deleted);
    }
}
