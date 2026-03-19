namespace EventBus.Messages
{
    public class CertificateCreatedEvent : IntegrationEvent
    {
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CertificateUrl { get; set; } = string.Empty;
        public string CertificateTitile { get; set; } = string.Empty;
        public string CertificateType { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
