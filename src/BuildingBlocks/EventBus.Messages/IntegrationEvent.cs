namespace EventBus.Messages
{
    public class IntegrationEvent
    {
        public int Id { get; init; }
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
        public string EventType => GetType().AssemblyQualifiedName!;
    }
}
