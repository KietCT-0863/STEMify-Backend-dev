using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Common.Interfaces.Repositories;

/// <summary>
/// Repository interface for ApplicationUser entity following Clean Architecture pattern.
/// Extends Identity-specific base interface for consistent CRUD operations and Specification pattern support.
/// Contains only truly user-specific methods that cannot be handled by base interface.
/// </summary>
public interface IUserRepository : IIdentityRepositoryBase<ApplicationUser, Guid>
{
    // Inherits all standard operations from IIdentityRepositoryBase<ApplicationUser, Guid>:
    // - FindByIdAsync, FindOneAsync, FindAsync
    // - GetAllAsync, FirstOrDefaultAsync
    // - AddAsync, UpdateAsync, DeleteAsync, DeleteByIdAsync
    // - CountAsync, AnyAsync
    // - Specification pattern support

    // User-specific methods that cannot be handled by base interface + Specifications

    /// <summary>
    /// Gets a user by their ID (alias for FindByIdAsync for compatibility)
    /// </summary>
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address (Identity-specific field)
    /// </summary>
    Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a user by their username (Identity-specific field)
    /// </summary>
    Task<ApplicationUser?> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets users by their role (TPT-specific query)
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all active users
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if an email is unique, optionally excluding a specific user
    /// </summary>
    Task<bool> IsEmailUniqueAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a username is unique, optionally excluding a specific user
    /// </summary>
    Task<bool> IsUserNameUniqueAsync(
        string userName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if an email exists in the system
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username exists in the system
    /// </summary>
    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);

}
