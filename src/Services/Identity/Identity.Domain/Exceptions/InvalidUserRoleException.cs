using Identity.Domain.Enums;
using Shared.Exceptions;

namespace Identity.Domain.Exceptions
{
    public class InvalidUserRoleException : DomainException
    {
        public InvalidUserRoleException(UserRole currentRole, string action)
            : base(
                $"User with role {currentRole} cannot perform action: {action}",
                "INVALID_USER_ROLE"
            ) { }
    }
}
