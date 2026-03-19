namespace Contracts.Abstractions.Persistence
{
    public interface IUnitOfWork : IDisposable
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface IUnitOfWork<out TContext> : IUnitOfWork
        where TContext : class
    {
        TContext Context { get; }
    }
}
