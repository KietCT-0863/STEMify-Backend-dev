using Contracts.Domains;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities
{
    public class Refund : EntityAuditBase<Guid>
    {
        public Guid PaymentId { get; set; }
        public string RefundNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public RefundStatus Status { get; set; } = RefundStatus.Pending;
        public string Reason { get; set; } = string.Empty;
        public string? ProviderRefundId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Payment Payment { get; set; } = null!;
    }
}
