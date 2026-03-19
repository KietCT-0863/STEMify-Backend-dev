using Contracts.Domains;
using Identity.Domain.Common;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Microsoft.AspNetCore.Identity;

namespace Identity.Domain.Entities;

/// <summary>
/// Abstract base class for ApplicationUser implementing TPT inheritance pattern
/// Inherits from IdentityUser<Guid> for ASP.NET Identity integration
/// Contains common user properties and business logic
/// </summary>
public abstract class ApplicationUser : IdentityUser<Guid>, IAggregateRoot<Guid>
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected ApplicationUser()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    } // For EF Core

    protected ApplicationUser(Guid id, string email, string userName, UserRole role)
    {
        Id = id;
        Email = email;
        UserName = userName;
        Role = role;
        Status = UserStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Domain properties
    public UserRole Role { get; protected set; }
    public UserStatus Status { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public DateTime? LastLoginAt { get; protected set; }
    public DateTime? EmailConfirmedAt { get; protected set; }

    // Domain events functionality
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    // Abstract properties for TPT - must be implemented by derived classes
    public abstract string FirstName { get; protected set; }
    public abstract string LastName { get; protected set; }
    public abstract string FullName { get; protected set; }

    // Common business methods
    public virtual void Activate(string? activatedBy = null)
    {
        if (Status == UserStatus.Active)
            return;
        if (Status == UserStatus.Deleted)
            throw new InvalidOperationException("Cannot activate deleted user");

        Status = UserStatus.Active;
        SetUpdatedAt();
        AddDomainEvent(
            new UserActivatedEvent(Id, Email ?? string.Empty, DateTime.UtcNow, activatedBy)
        );
    }

    public virtual void Login(string? ipAddress = null, string? userAgent = null)
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException("Only active users can log in");

        LastLoginAt = DateTime.UtcNow;
        SetUpdatedAt();
        // Note: UserLoggedInEvent is now handled at Application layer
    }

    public virtual void DeactivateUser()
    {
        if (Status == UserStatus.Disabled)
            return;

        Status = UserStatus.Disabled;
        SetUpdatedAt();
    }

    public virtual void ConfirmEmail()
    {
        if (EmailConfirmedAt.HasValue)
            return;

        EmailConfirmed = true;
        EmailConfirmedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    protected void SetUpdatedAt()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimestamp()
    {
        SetUpdatedAt();
    }

    public void UpdateName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));

        FirstName = firstName;
        LastName = lastName;
    }

    public void UpdateRole(UserRole role)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
            throw new ArgumentException("Invalid user role", nameof(role));

        Role = role;
    }

    public void UpdateStatus(UserStatus status)
    {
        if (!Enum.IsDefined(typeof(UserStatus), status))
            throw new ArgumentException("Invalid user status", nameof(status));

        Status = status;
    }

    // Business logic methods
    public bool IsActive() => Status == UserStatus.Active;

    public bool IsEmailConfirmed() => EmailConfirmed;

    public bool CanLogin() => Status == UserStatus.Active 
   // && IsEmailConfirmed()
    ;
}
