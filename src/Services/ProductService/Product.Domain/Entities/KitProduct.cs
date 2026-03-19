using Contracts.Domains;
using Product.Domain.Enums;

namespace Product.Domain.Entities
{
    public class KitProduct : EntityAuditBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CreatedByUserId { get; set; }
        public decimal Weight { get; set; }
        public string? Dimensions { get; set; }
        public int AgeRangeId { get; set; }
        public KitProductStatus Status { get; set; } = KitProductStatus.Draft;

        // Navigation properties
        public virtual ICollection<KitComponent> KitComponents { get; set; } = [];
        public virtual ICollection<KitImage> KitImages { get; set; } = new List<KitImage>();
    }
}
