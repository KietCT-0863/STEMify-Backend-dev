using Ardalis.Specification;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Common.Specifications
{
    public class UserByEmailSpecification : Specification<ApplicationUser>
    {
        public UserByEmailSpecification(string email)
        {
            Query.Where(u => u.Email == email);
        }
    }

    public class UserByUserNameSpecification : Specification<ApplicationUser>
    {
        public UserByUserNameSpecification(string userName)
        {
            Query.Where(u => u.UserName == userName);
        }
    }

    public class ActiveUsersSpecification : Specification<ApplicationUser>
    {
        public ActiveUsersSpecification()
        {
            Query.Where(u => u.Status == UserStatus.Active).OrderBy(u => u.UserName);
        }
    }

    public class UsersByRoleSpecification : Specification<ApplicationUser>
    {
        public UsersByRoleSpecification(UserRole role)
        {
            Query.Where(u => u.Role == role).OrderBy(u => u.CreatedAt);
        }
    }

    public class UniqueEmailSpecification : Specification<ApplicationUser>
    {
        public UniqueEmailSpecification(string email, Guid? excludeUserId = null)
        {
            if (excludeUserId.HasValue)
            {
                Query.Where(u => u.Email == email && u.Id != excludeUserId.Value);
            }
            else
            {
                Query.Where(u => u.Email == email);
            }
        }
    }

    public class UniqueUserNameSpecification : Specification<ApplicationUser>
    {
        public UniqueUserNameSpecification(string userName, Guid? excludeUserId = null)
        {
            if (excludeUserId.HasValue)
            {
                Query.Where(u => u.UserName == userName && u.Id != excludeUserId.Value);
            }
            else
            {
                Query.Where(u => u.UserName == userName);
            }
        }
    }

    public class UserWithProfileSpecification : Specification<ApplicationUser>
    {
        public Guid UserId { get; }

        public UserWithProfileSpecification(Guid userId)
        {
            UserId = userId;
            Query.Where(u => u.Id == userId);
        }
    }

    public class UserByIdSpecification : Specification<ApplicationUser>
    {
        public Guid UserId { get; }

        public UserByIdSpecification(Guid userId)
        {
            UserId = userId;
            Query.Where(u => u.Id == userId);
        }
    }

}
