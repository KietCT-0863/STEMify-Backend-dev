using Contracts.Domains;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities
{
    public class PaymentTransaction : EntityAuditBase<Guid>
    {
        public Guid PaymentId { get; set; }
        public string ProviderTransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? GatewayResponseCode { get; set; }
        public string? GatewayResponseMessage { get; set; }
        public string? RawResponse { get; set; } // Store full gateway response
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public Payment Payment { get; set; } = null!;
    }
}
