using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Concrete implementation of ApplicationUser for general users (Admin, Staff, Guest)
/// Used for roles that don't require specialized profiles
/// </summary>
public class User : ApplicationUser
{
    private User()
        : base() { } // For EF Core

    private User(
        Guid id,
        string email,
        string userName,
        string firstName,
        string lastName,
        UserRole role
    )
        : base(id, email, userName, role)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdateFullName();

        AddDomainEvent(new UserCreatedEvent(Id, email, userName, role.ToString(), DateTime.UtcNow));
    }

    public override string FirstName { get; protected set; } = string.Empty;
    public override string LastName { get; protected set; } = string.Empty;
    public override string FullName { get; protected set; } = string.Empty;

    private void UpdateFullName()
    {
        FullName = $"{FirstName} {LastName}".Trim();
    }

    public static User Create(
        Guid id,
        string email,
        string userName,
        string firstName,
        string lastName,
        UserRole role
    )
    {
        
        return new User(id, email, userName, firstName, lastName, role);
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdateFullName();
        SetUpdatedAt();

        AddDomainEvent(
            new UserProfileUpdatedEvent(Id, "User", $"FirstName,LastName", DateTime.UtcNow)
        );
    }
}
