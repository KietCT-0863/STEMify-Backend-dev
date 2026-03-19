using Contracts.Abstractions.Event;
using Contracts.Common.Domain;
using Contracts.Domains;

namespace Identity.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// Provides common properties and domain events functionality
/// </summary>
public abstract class BaseEntity<TKey> : EntityBase<TKey>, IAggregateRoot<TKey>, IAggregateBase
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected BaseEntity() { }

    protected BaseEntity(TKey id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    // Implement IAggregateBase interface
    public void AddDomainEvents(IDomainEvent domainEvent)
    {
        if (domainEvent is DomainEvent de)
        {
            _domainEvents.Add(de);
        }
    }

    public bool HasUncommittedDomainEvents()
    {
        return _domainEvents.Count > 0;
    }

    public IReadOnlyList<IDomainEvent> GetUncommittedDomainEvents()
    {
        return _domainEvents.Cast<IDomainEvent>().ToList().AsReadOnly();
    }

    public IReadOnlyList<IDomainEvent> DequeueUncommittedDomainEvents()
    {
        var events = _domainEvents.Cast<IDomainEvent>().ToList().AsReadOnly();
        _domainEvents.Clear();
        return events;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new InvalidOperationException($"Business rule broken: {rule.Message}");
        }
    }
}
