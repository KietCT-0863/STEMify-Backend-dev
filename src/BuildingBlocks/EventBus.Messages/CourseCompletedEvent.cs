namespace EventBus.Messages
{
    public class CourseCompletedEvent : IntegrationEvent
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
