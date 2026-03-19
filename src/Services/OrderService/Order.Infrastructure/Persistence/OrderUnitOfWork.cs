using Contracts.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Repositories;

namespace Order.Infrastructure.Persistence
{
    public class OrderUnitOfWork : IEfUnitOfWork<OrderDbContext>, IOrderUnitOfWork
    {
        private readonly OrderDbContext _context;

        public OrderDbContext DbContext => _context;

        public IOrderRepository Orders { get; }
        public IContractRepository Contracts { get; }
        public IOrganizationRepository Organizations { get; }
        public ILicenseAssignmentRepository LicenseAssignments { get; }
        public IOrganizationSubscriptionOrderRepository OrganizationSubscriptionOrders { get; }
        public IOrganizationTypeRepository OrganizationTypes { get; }
        public ISubscriptionOrderCurriculumRepository SubscriptionOrderCurriculums { get; }

        /// <summary>
        /// Constructor with dependency injection for all repositories
        /// </summary>
        /// <param name="context">Database context</param>
        public OrderUnitOfWork(
            OrderDbContext context,
            IOrderRepository OrderRepository,
            IContractRepository ContractRepository,
            IOrganizationRepository OrganizationRepository,
            ILicenseAssignmentRepository LicenseAssignmentRepository,
            IOrganizationSubscriptionOrderRepository OrganizationSubscriptionOrderRepository,
            ISubscriptionOrderCurriculumRepository SubscriptionOrderCurriculumRepository,
            IOrganizationTypeRepository OrganizationTypeRepository
        )
        {
            _context = context;
            Orders = OrderRepository;
            Contracts = ContractRepository;
            Organizations = OrganizationRepository;
            LicenseAssignments = LicenseAssignmentRepository;
            OrganizationSubscriptionOrders = OrganizationSubscriptionOrderRepository;
            OrganizationTypes = OrganizationTypeRepository;
            SubscriptionOrderCurriculums = SubscriptionOrderCurriculumRepository;
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
