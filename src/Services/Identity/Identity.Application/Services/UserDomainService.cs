using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;

namespace Identity.Application.Services;

public class UserDomainService : IUserDomainService
{
    public bool CanChangeRole(UserRole currentRole, UserRole newRole, UserRole requestedByRole)
    {
        // Business rules for role changes

        // Same role - no change needed
        if (currentRole == newRole)
            return true;

        // Only Admin can promote to Admin
        if (newRole == UserRole.Admin && requestedByRole != UserRole.Admin)
            return false;

        // Only Admin can demote from Admin
        if (currentRole == UserRole.Admin && requestedByRole != UserRole.Admin)
            return false;

        // Admin can change any role
        if (requestedByRole == UserRole.Admin)
            return true;

            // Staff can promote Member to Staff
        if (requestedByRole == UserRole.Staff)
        {
            return currentRole == UserRole.Member
                && (newRole == UserRole.Staff);
        }

        return false;
    }

    public bool CanAccessResource(UserRole userRole, string resourceName)
    {
        // Business rules for resource access
        return userRole switch
        {
            UserRole.Admin => true, // Admin can access everything
            UserRole.Staff => IsStaffResource(resourceName),
            UserRole.Member => IsMemberResource(resourceName),
            _ => false,
        };
    }

    public bool IsProfileRequired(UserRole role)
    {
        return role == UserRole.Member;
    }

    public bool CanActivateUser(UserStatus currentStatus, UserRole role)
    {
        
        return currentStatus switch
        {
            UserStatus.Pending => true, 
            UserStatus.Disabled => role != UserRole.Member, 
            UserStatus.Locked => true, 
            UserStatus.Active => false, 
            UserStatus.Deleted => false, 
            _ => false,
        };
    }

    public bool IsEmailValid(string email, string userName)
    {
        // Business rule: Some email domains might be restricted
        var restrictedDomains = new[] { "temp-mail.org", "10minutemail.com" };

        try
        {
            var emailObj = Email.Create(email);
            var domain = emailObj.Value.Split('@')[1];

            if (restrictedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsStrongPassword(string password)
    {
        // Business rules for password strength
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static bool IsStaffResource(string resourceName)
    {
        var staffResources = new[]
        {
            "UserManagement",
            "ProfileManagement",
            "Reports",
            "SystemSettings",
        };

        return staffResources.Contains(resourceName);
    }

    private static bool IsMemberResource(string resourceName)
    {
        var memberResources = new[]
        {
            "MemberProfile",
            "SelfService",
            "Notifications",
        };

        return memberResources.Contains(resourceName);
    }

}
