using Contracts.Domains;

namespace Identity.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots in the domain
/// Provides domain events functionality
/// </summary>
public interface IAggregateRoot<TKey>
{
    TKey Id { get; }
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
