using Contracts.Abstractions.Persistence.EfCore;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Identity Unit of Work implementation with simplified repository pattern for TPT
/// </summary>
public class IdentityUnitOfWork : IEfUnitOfWork<ApplicationDbContext>, IIdentityUnitOfWork
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// User repository - handles all user types via TPT inheritance
    /// </summary>
    public IUserRepository Users { get; }
    public IContactRepository Contacts { get; init; }
    public IJobRoleRepository JobRoles { get; init; }

    public ApplicationDbContext DbContext => _context;

    /// <summary>
    /// Constructor with dependency injection for user repository
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="userRepository">User repository handling TPT inheritance</param>
    public IdentityUnitOfWork(
        ApplicationDbContext context, 
        IUserRepository userRepository,
        IContactRepository contacts,
        IJobRoleRepository jobRoles)
    {
        _context = context;
        Users = userRepository;
        Contacts = contacts;
        JobRoles = jobRoles;
    }

    public DbSet<TEntity> Set<TEntity>()
        where TEntity : class
    {
        return _context.Set<TEntity>();
    }

    public Task BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default
    )
    {
        return _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }

    public Task RetryOnExceptionAsync(Func<Task> operation)
    {
        // Simple implementation - just execute the operation
        return operation();
    }

    public Task<TResult> RetryOnExceptionAsync<TResult>(Func<Task<TResult>> operation)
    {
        // Simple implementation - just execute the operation
        return operation();
    }

    public async Task ExecuteTransactionalAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default
    )
    {

        var strategy = _context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<T> ExecuteTransactionalAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default
    )
    {

        var strategy = _context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
