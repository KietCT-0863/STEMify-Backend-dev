namespace EventBus.Messages.Payment
{
    public class PaymentFailedEvent : IntegrationEvent
    {
        public Guid PaymentId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid BuyerId { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string PaymentProvider { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
    }
}
