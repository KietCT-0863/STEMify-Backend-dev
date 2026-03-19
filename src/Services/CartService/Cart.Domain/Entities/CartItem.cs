using System.ComponentModel.DataAnnotations;

namespace Cart.Domain.Entities
{
    public class CartItem
    {
        [Required]
        public int CartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        // Navigation property
        public virtual Cart Cart { get; set; }
    }
}
