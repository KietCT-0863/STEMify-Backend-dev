using Contracts.Domains;

namespace Product.Domain.Entities
{
    public class KitImage : EntityBase<int>
    {
        public int KitId { get; set; }
        public string? ImageUrl { get; set; }
        public string? AltText { get; set; }

        public virtual KitProduct Kit { get; set; } = null!;
    }
}
