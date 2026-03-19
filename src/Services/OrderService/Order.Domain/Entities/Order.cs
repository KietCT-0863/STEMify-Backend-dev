using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class Order : EntityAuditBase<int>
    {
        public Guid BuyerId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public decimal Amount { get; set; }

        public Guid? PaymentId { get; set; }

        public ICollection<OrderHistory> OrderHistories { get; set; } = [];
        public ICollection<OrderItem> OrderItems { get; set; } = [];
    }
}
