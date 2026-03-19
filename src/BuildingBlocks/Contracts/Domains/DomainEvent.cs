using Contracts.Abstractions.Event;
using System.ComponentModel.DataAnnotations;

namespace Contracts.Domains;

public abstract record DomainEvent : IDomainEvent
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    // Implement IEvent
    public Guid EventId => Id;
    public long EventVersion { get; init; } = 1;
    public DateTimeOffset TimeStamp { get; init; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().Name;

    // Implement IDomainEvent
    public dynamic AggregateId { get; init; } = Guid.Empty;
    public long AggregateSequenceNumber { get; init; } = 0;

    public IDomainEvent WithAggregate(dynamic aggregateId, long version)
    {
        return this with
        {
            AggregateId = aggregateId,
            AggregateSequenceNumber = version
        };
    }
}
