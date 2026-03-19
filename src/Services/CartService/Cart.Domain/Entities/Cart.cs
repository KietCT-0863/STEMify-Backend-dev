using Contracts.Domains;
using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Cart.Domain.Entities
{
    public class Cart : EntityAuditBase<int>
    {
        [Required]
        public string UserId { get; set; }
        [Required]
        public CartStatus Status { get; set; } = CartStatus.Active;

        // Navigation property
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
