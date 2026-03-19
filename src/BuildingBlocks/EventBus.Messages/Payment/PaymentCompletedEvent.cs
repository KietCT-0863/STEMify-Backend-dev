namespace EventBus.Messages.Payment
{
    public class PaymentCompletedEvent : IntegrationEvent
    {
        public Guid PaymentId { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid BuyerId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string ProviderTransactionId { get; set; } = string.Empty;
        public string PaymentProvider { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}
