using Ardalis.Specification;
using Contracts.Abstractions.Paging;
using Contracts.Common.Interfaces.Paging;
using Contracts.Domains;
using System.Linq.Expressions;

namespace Contracts.Abstractions.Persistence
{
    /// <summary>
    /// Interface for read-only repository operations
    /// </summary>
    public interface IRepositoryQueryBase<TEntity, TId>
        where TEntity : EntityBase<TId>
    {
        Task<TEntity?> FindByIdAsync(TId id, CancellationToken cancellationToken = default);

        Task<TEntity?> FindOneAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TEntity>> GetAllAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        );

        Task<TEntity?> FirstOrDefaultAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        );

        Task<TResult?> FirstOrDefaultAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        );

        Task<TEntity?> SingleOrDefaultAsync(
            ISingleResultSpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        );

        Task<TResult?> SingleOrDefaultAsync<TResult>(
            ISingleResultSpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        );

        Task<int> CountAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        );

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<bool> AnyAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        );
        Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );

        IQueryable<TResult> ProjectBy<TResult, TSortKey>(
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null
        )
            where TResult : class;

        Task<IPageList<TEntity>> GetByPageFilter<TSortKey>(
            IPageRequest pageRequest,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        );
        Task<IPageList<TResult>> GetByPageFilter<TSortKey, TResult>(
            IPageRequest pageRequest,
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        )
            where TResult : class;
        Task<IPageList<TResult>> GetByPageFilter<TSortKey, TResult>(
            IPageRequest pageRequest,
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            bool descending = true,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        )
            where TResult : class;
    }

    public interface IRepositoryWriteBase<TEntity, in TId>
        where TEntity : EntityBase<TId>
    {
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken = default);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(
            IReadOnlyList<TEntity> entities,
            CancellationToken cancellationToken = default
        );
        Task DeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        );
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteByIdAsync(TId id, CancellationToken cancellationToken = default);
    }

    public interface IRepositoryBaseAsync<TEntity, TId>
        : IRepositoryQueryBase<TEntity, TId>,
            IRepositoryWriteBase<TEntity, TId>,
            IDisposable
        where TEntity : EntityBase<TId>;

    // public interface IRepositoryBaseAsync<T, K> : IRepositoryQueryBase<T, K> where T : EntityBase<K>
    // {
    //     // Specification Pattern Methods - Complete Integration
    //     Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec);
    //     Task<IEnumerable<T>> GetEntitiesWithSpecAsync(ISpecification<T> spec);
    //     Task<int> CountAsync(ISpecification<T> spec);
    //     Task<int> CountAsync();

    //     // Basic CRUD Operations - NO SaveChanges (handled by Unit of Work)
    //     Task<K> CreateAsync(T entity);
    //     Task<IList<K>> CreateListAsync(IEnumerable<T> entities);
    //     Task UpdateAsync(T entity);
    //     Task UpdateListAsync(IEnumerable<T> entities);
    //     Task DeleteAsync(T entity);
    //     Task DeleteListAsync(IEnumerable<T> entities);

    //     // REMOVED: Transaction methods - these belong to Unit of Work
    //     // Task<IDbContextTransaction> BeginTransactionAsync();
    //     // Task EndTransactionAsync();
    //     // Task RollBackTransactionAsync();
    // }
}
