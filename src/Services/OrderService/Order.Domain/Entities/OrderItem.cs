using Contracts.Domains;

namespace Order.Domain.Entities
{
    public class OrderItem : EntityBase<int>
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
        public Order Order { get; set; } = null!;
    }
}
