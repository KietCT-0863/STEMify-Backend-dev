namespace EventBus.Messages
{
    public class CertificateGenerationRequestedEvent : IntegrationEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string CertificateType { get; set; }
        public int? CourseEnrollmentId { get; set; }
        public int? CurriculumEnrollmentId { get; set; }
    }
}
