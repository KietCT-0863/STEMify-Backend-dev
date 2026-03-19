namespace EventBus.Messages.Resource
{
    public class CurriculumUpdatedEvent : IntegrationEvent
    {
        public string Title { get; set; }
        public string Status { get; set; }
        public string CreatedByUserId { get; set; }
    }
}
