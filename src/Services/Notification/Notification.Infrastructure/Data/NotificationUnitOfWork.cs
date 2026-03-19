using Contracts.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Common.Interfaces;
using Notification.Application.Common.Interfaces.Repositories;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.Data
{
    public class NotificationUnitOfWork
        : IEfUnitOfWork<NotificationDbContext>,
            INotificationUnitOfWork
    {
        private readonly NotificationDbContext _context;

        public NotificationDbContext DbContext => _context;

        /// <summary>
        /// Notification repository - injected via DI
        /// </summary>
        public INotificationRepository Notifications { get; }

        /// <summary>
        /// Notification repository - injected via DI
        /// </summary>
        public IDeviceRepository Devices { get; }

        /// <summary>
        /// Constructor with dependency injection for all repositories
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="notificationRepository">Notification repository</param>
        public NotificationUnitOfWork(
            NotificationDbContext context,
            INotificationRepository notificationRepository,
            IDeviceRepository deviceRepository
        )
        {
            _context = context;
            Notifications = notificationRepository;
            Devices = deviceRepository;
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
            using var transaction = await _context.Database.BeginTransactionAsync(
                cancellationToken
            );
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
        }

        public async Task<T> ExecuteTransactionalAsync<T>(
            Func<Task<T>> action,
            CancellationToken cancellationToken = default
        )
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                cancellationToken
            );
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
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
