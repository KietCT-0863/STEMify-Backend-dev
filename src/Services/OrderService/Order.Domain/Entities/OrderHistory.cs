using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class OrderHistory : EntityBase<int>
    {
        public int OrderId { get; set; }
        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }
        public string ChangedById { get; set; } = string.Empty;
        public string ChangedByRole { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Notes { get; set; }
        public Order Order { get; set; } = null!;
    }
}
