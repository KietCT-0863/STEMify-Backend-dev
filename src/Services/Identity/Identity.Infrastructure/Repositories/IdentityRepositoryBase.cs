using System.Linq.Expressions;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation for Identity entities that inherit from IdentityUser
/// Provides consistent CRUD operations and Specification pattern support for Clean Architecture
/// </summary>
/// <typeparam name="TEntity">Entity type that inherits from ApplicationUser</typeparam>
/// <typeparam name="TId">Primary key type</typeparam>
public class IdentityRepositoryBase<TEntity, TId> : IIdentityRepositoryBase<TEntity, TId>
    where TEntity : ApplicationUser
    where TId : notnull
{
    protected readonly ApplicationDbContext _context;
    protected readonly UserManager<ApplicationUser> _userManager;
    protected readonly DbSet<TEntity> _dbSet;

    public IdentityRepositoryBase(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager
    )
    {
        _context = context;
        _userManager = userManager;
        _dbSet = context.Set<TEntity>();
    }

    // Basic CRUD operations
    public virtual async Task<TEntity?> FindByIdAsync(
        TId id,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        var result = await _dbSet.ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    // Specification pattern support
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.WithSpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _dbSet.WithSpecification(specification).ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.WithSpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(
        ISpecification<TEntity, TResult> specification,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _dbSet.WithSpecification(specification).ToListAsync(cancellationToken);
        return result.AsReadOnly();
    }

    // Count and existence checks
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.WithSpecification(specification).CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default
    )
    {
        return await _dbSet.WithSpecification(specification).AnyAsync(cancellationToken);
    }

    // Write operations
    public virtual async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        // For Identity entities, use UserManager for proper Identity integration
        var result = await _userManager.CreateAsync(entity);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return entity;
    }

    public virtual async Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        // Update using UserManager for proper Identity integration
        var result = await _userManager.UpdateAsync(entity);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }
        return entity;
    }

    public virtual async Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _userManager.DeleteAsync(entity);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete user: {errors}");
        }
    }

    public virtual async Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await FindByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            throw new InvalidOperationException($"User with ID {id} not found");
        }

        await DeleteAsync(entity, cancellationToken);
    }
}
