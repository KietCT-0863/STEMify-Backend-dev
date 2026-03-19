using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ApplicationUser entity following Clean Architecture pattern
/// Extends the base Identity repository with user-specific methods that cannot be handled by base interface
/// </summary>
public class UserRepository : IdentityRepositoryBase<ApplicationUser, Guid>, IUserRepository
{
    public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        : base(context, userManager) { }

    // User-specific methods that cannot be handled by base interface + Specifications

    public async Task<ApplicationUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<ApplicationUser?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default
    )
    {
        return await _userManager.FindByNameAsync(userName);
    }

    public async Task<IEnumerable<ApplicationUser>> GetUsersByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default
    )
    {
        var normalizedRole = role switch
        {
            UserRole.Admin => UserRole.Admin,
            UserRole.Staff => UserRole.Staff,
            _ => UserRole.Member,
        };

        return await _context
            .Set<ApplicationUser>()
            .Where(u => u.Role == normalizedRole)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApplicationUser>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Set<ApplicationUser>()
            .Where(u => u.Status == UserStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task<bool> UserNameExistsAsync(
        string userName,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _userManager.FindByNameAsync(userName);
        return user != null;
    }

    // CRUD operations are inherited from IdentityRepositoryBase

    // Validation methods
    public async Task<bool> IsEmailUniqueAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.Set<ApplicationUser>().Where(u => u.Email == email);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsUserNameUniqueAsync(
        string userName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default
    )
    {
        var query = _context.Set<ApplicationUser>().Where(u => u.UserName == userName);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return !await query.AnyAsync(cancellationToken);
    }

}
