using Contracts.Domains;
using Payment.Domain.Enums;

namespace Payment.Domain.Entities
{
    public class Payment : EntityAuditBase<Guid>
    {
        public int OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public PaymentProvider Provider { get; set; }
        public PaymentMethod Method { get; set; }
        public string? PaymentUrl { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Metadata { get; set; } // JSON for additional data
        public ICollection<PaymentTransaction> Transactions { get; set; } = [];
        public ICollection<Refund> Refunds { get; set; } = [];
    }
}
