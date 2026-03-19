namespace EventBus.Messages
{
    public class StudentCreatedEvent : IntegrationEvent
    {
        public Guid Id { get; set; } = default!;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
