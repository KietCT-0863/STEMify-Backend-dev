namespace EventBus.Messages
{
    public class CurricullumCompletedEvent : IntegrationEvent
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CurriculumId { get; set; }
        public string CurriculumTitle { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
