using Identity.Domain.Enums;

namespace Identity.Application.Services;

public interface IUserDomainService
{
    /// <summary>
    /// Validates if a user can change their role
    /// </summary>
    bool CanChangeRole(UserRole currentRole, UserRole newRole, UserRole requestedByRole);

    /// <summary>
    /// Validates if a user can access specific resources based on their role
    /// </summary>
    bool CanAccessResource(UserRole userRole, string resourceName);

    /// <summary>
    /// Determines if a profile is required for the given role
    /// </summary>
    bool IsProfileRequired(UserRole role);

    /// <summary>
    /// Validates business rules for user activation
    /// </summary>
    bool CanActivateUser(UserStatus currentStatus, UserRole role);

    /// <summary>
    /// Validates email according to business rules
    /// </summary>
    bool IsEmailValid(string email, string userName);

    /// <summary>
    /// Validates password strength according to business rules
    /// </summary>
    bool IsStrongPassword(string password);
}
