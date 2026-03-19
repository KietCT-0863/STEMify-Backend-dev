namespace EventBus.Messages
{
    public class LessonCompletedEvent : IntegrationEvent
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int LessonId { get; set; }
        public string LessonName { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
