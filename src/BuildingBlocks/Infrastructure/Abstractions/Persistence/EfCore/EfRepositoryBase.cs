using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Contracts.Abstractions.Paging;
using Contracts.Abstractions.Persistence;
using Contracts.Common.Interfaces.Paging;
using Contracts.Domains;
using Infrastructure.Abstractions.Paging;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Sieve.Services;
using System.Linq.Expressions;

namespace Infrastructure.Abstractions.Persistence.EfCore
{
    public abstract class EfRepositoryBase<TDbContext, TEntity, TKey>(
        TDbContext dbContext,
        ISieveProcessor sieveProcessor
    ) : IRepositoryBaseAsync<TEntity, TKey>
        where TEntity : EntityBase<TKey>
        where TDbContext : DbContext
    {
        private readonly SpecificationEvaluator _specificationEvaluator =
            SpecificationEvaluator.Default;
        protected DbSet<TEntity> DbSet { get; } = dbContext.Set<TEntity>();
        protected TDbContext DbContext { get; } = dbContext;

        public Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return DbSet.AsNoTracking().SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
        }

        public Task<TEntity?> FindByIdForUpdateAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return DbSet.AsTracking().SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
        }

        public Task<TEntity?> FindOneAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(predicate);

            return DbSet.AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        )
        {
            return await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification, true).AnyAsync(cancellationToken);
        }

        public async Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        )
        {
            return await DbSet.AnyAsync(predicate, cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        )
        {
            var queryResult = await ApplySpecification(specification)
                .ToListAsync(cancellationToken);

            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(queryResult).ToList();
        }

        public async Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        )
        {
            var queryResult = await ApplySpecification(specification)
                .ToListAsync(cancellationToken);

            return specification.PostProcessingAction is null
                ? queryResult
                : specification.PostProcessingAction(queryResult).ToList();
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> FirstOrDefaultAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TEntity?> SingleOrDefaultAsync(
            ISingleResultSpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<TResult?> SingleOrDefaultAsync<TResult>(
            ISingleResultSpecification<TEntity, TResult> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CountAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default
        )
        {
            return await ApplySpecification(specification, true).CountAsync(cancellationToken);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            return await DbSet.CountAsync(cancellationToken);
        }

        public IQueryable<TResult> ProjectBy<TResult, TSortKey>(
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null
        )
            where TResult : class
        {
            var query = DbSet.AsNoTracking().AsQueryable();
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (sortExpression is not null)
            {
                query = query.OrderByDescending(sortExpression);
            }

            return projectionFunc(query);
        }

        public async Task<IPageList<TEntity>> GetByPageFilter<TSortKey>(
            IPageRequest pageRequest,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        )
        {
            var query = DbSet.AsNoTracking().AsQueryable();
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (sortExpression is not null)
            {
                query = query.OrderByDescending(sortExpression);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
                .Take(pageRequest.PageSize)
                .ToListAsync(cancellationToken);

            return new PageList<TEntity>(
                items,
                pageRequest.PageNumber,
                pageRequest.PageSize,
                totalCount
            );
        }

        public async Task<IPageList<TResult>> GetByPageFilter<TSortKey, TResult>(
            IPageRequest pageRequest,
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        )
            where TResult : class
        {
            var query = DbSet.AsNoTracking().AsQueryable();
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (sortExpression is not null)
            {
                query = query.OrderByDescending(sortExpression);
            }

            var projectedQuery = projectionFunc(query);
            var totalCount = await projectedQuery.CountAsync(cancellationToken);
            var items = await projectedQuery
                .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
                .Take(pageRequest.PageSize)
                .ToListAsync(cancellationToken);

            return new PageList<TResult>(
                items,
                pageRequest.PageNumber,
                pageRequest.PageSize,
                totalCount
            );
        }

        public async Task<IPageList<TResult>> GetByPageFilter<TSortKey, TResult>(
            IPageRequest pageRequest,
            Func<IQueryable<TEntity>, IQueryable<TResult>> projectionFunc,
            Expression<Func<TEntity, TSortKey>>? sortExpression = null,
            bool descending = true,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default
        )
            where TResult : class
        {
            var query = DbSet.AsNoTracking().AsQueryable();
            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (sortExpression is not null)
            {
                query = descending
                    ? query.OrderByDescending(sortExpression)
                    : query.OrderBy(sortExpression);
            }

            var projectedQuery = projectionFunc(query);
            var totalCount = await projectedQuery.CountAsync(cancellationToken);
            var items = await projectedQuery
                .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
                .Take(pageRequest.PageSize)
                .ToListAsync(cancellationToken);

            return new PageList<TResult>(
                items,
                pageRequest.PageNumber,
                pageRequest.PageSize,
                totalCount
            );
        }

        public async Task<TEntity> AddAsync(
            TEntity entity,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(entity);

            await DbSet.AddAsync(entity, cancellationToken);

            return entity;
        }

        public async Task AddRangeAsync(
            IReadOnlyList<TEntity> entities,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(entities);

            foreach (var entity in entities)
            {
                await AddAsync(entity, cancellationToken);
            }
        }

        public Task<TEntity> UpdateAsync(
            TEntity entity,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(entity);

            var entry = DbContext.Entry(entity);
            entry.State = EntityState.Modified;

            return Task.FromResult(entry.Entity);
        }

        public async Task DeleteRangeAsync(
            IReadOnlyList<TEntity> entities,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(entities);

            foreach (var entity in entities)
            {
                await DeleteAsync(entity, cancellationToken);
            }
        }

        public Task DeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        )
        {
            var items = DbSet.Where(predicate).ToList();

            return DeleteRangeAsync(items, cancellationToken);
        }

        public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            DbSet.Remove(entity);

            return Task.CompletedTask;
        }

        public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            var item = await DbSet.AsTracking().SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
            if (item is null)
                throw new NotFoundException($"Item with ID '{id}' not found");

            DbSet.Remove(item);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private IQueryable<TEntity> ApplySpecification(
            ISpecification<TEntity> specification,
            bool evaluateCriteriaOnly = false
        ) =>
            _specificationEvaluator.GetQuery(
                DbContext.Set<TEntity>().AsQueryable(),
                specification,
                evaluateCriteriaOnly
            );

        private IQueryable<TResult> ApplySpecification<TResult>(
            ISpecification<TEntity, TResult> specification
        ) =>
            _specificationEvaluator.GetQuery(DbContext.Set<TEntity>().AsQueryable(), specification);
    }
}
