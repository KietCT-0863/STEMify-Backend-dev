namespace EventBus.Messages.Resource
{
    public class CourseCreatedEvent : IntegrationEvent
    {
        public int CourseId { get; set; }
        public string Title { get; set; }
        public string CreatedByUserId { get; set; }
    }
}
