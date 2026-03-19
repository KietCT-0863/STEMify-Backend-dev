namespace Contracts.Abstractions.Event
{
    public interface IDomainEventsAccessor
    {
        IReadOnlyList<IDomainEvent> DequeueUncommittedDomainEvents();
    }
}
