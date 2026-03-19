namespace EventBus.Messages.Resource
{
    public class CourseUpdatedEvent : IntegrationEvent
    {
        public string Title { get; set; }
        public string Status { get; set; }
        public string CreatedByUserId { get; set; }
    }
}
