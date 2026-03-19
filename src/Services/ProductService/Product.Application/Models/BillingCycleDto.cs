namespace Product.Application.Models
{
    public class BillingCycleDto
    {
        public Product.Domain.Enums.BillingCycle BillingCycle { get; set; }
        public decimal Price { get; set; }
    }
}
