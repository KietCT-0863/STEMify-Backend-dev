namespace EventBus.Messages.Resource
{
    public class CurriculumCreatedEvent : IntegrationEvent
    {
        public int CurriculumId { get; set; }
        public string Title { get; set; }
        public string CreatedByUserId { get; set; }
    }
}
