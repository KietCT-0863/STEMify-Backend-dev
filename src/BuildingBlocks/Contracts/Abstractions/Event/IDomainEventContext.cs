namespace Contracts.Abstractions.Event
{
    public interface IDomainEventContext
    {
        IReadOnlyList<IDomainEvent> DequeueUncommittedDomainEvents();
    }
}
