using Contracts.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Repositories;

namespace Product.Infrastructure.Persistence
{
    public class ProductUnitOfWork : IEfUnitOfWork<ProductDbContext>, IProductUnitOfWork
    {
        private readonly ProductDbContext _context;

        public ProductDbContext DbContext => _context;

        public IPlanRepository Plans { get; }
        public IPlanBillingCycleRepository PlanBillingCycles { get; }
        public IPlanCurriculumRepository PlanCurriculums { get; }
        public IKitProductRepository KitProducts { get; }
        public IKitImageRepository KitImages { get; }
        public IComponentRepository Components { get; }
        public IKitComponentRepository KitComponents { get; }


        /// <summary>
        /// Constructor with dependency injection for all repositories
        /// </summary>
        /// <param name="context">Database context</param>
        /// <param name="annoucementRepository">Annoucement repository</param>
        /// <param name="classroomRepository">Classroom repository</param>
        public ProductUnitOfWork(
            ProductDbContext context,
            IPlanRepository planRepository,
            IPlanBillingCycleRepository planBillingCycleRepository,
            IPlanCurriculumRepository planCurriculumRepository,
            IKitProductRepository kitProductRepository,
            IKitImageRepository kitImageRepository,
            IComponentRepository componentRepository,
            IKitComponentRepository kitComponentRepository
        )
        {
            _context = context;
            Plans = planRepository;
            PlanBillingCycles = planBillingCycleRepository;
            PlanCurriculums = planCurriculumRepository;
            KitProducts = kitProductRepository;
            KitImages = kitImageRepository;
            Components = componentRepository;
            KitComponents = kitComponentRepository;
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
