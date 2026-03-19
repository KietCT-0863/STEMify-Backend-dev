using Contracts.Domains;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Contracts.Abstractions.Persistence.EfCore
{
    public interface IEfRepository<TEntity, TId> : IRepositoryBaseAsync<TEntity, TId>
        where TEntity : EntityBase<TId>
    {
        IEnumerable<TEntity> GetInclude(
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null
        );

        IEnumerable<TEntity> GetInclude(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
            bool withTracking = true
        );

        Task<IEnumerable<TEntity>> GetIncludeAsync(
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null
        );

        Task<IEnumerable<TEntity>> GetIncludeAsync(
            Expression<Func<TEntity, bool>> predicate,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? includes = null,
            bool withTracking = true
        );
    }
}
