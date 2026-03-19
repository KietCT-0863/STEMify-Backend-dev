namespace EventBus.Messages
{
    public class CurriculumEnrollmentCreatedEvent
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int CurriculumId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string CurriculumTitle { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }
    }
}
