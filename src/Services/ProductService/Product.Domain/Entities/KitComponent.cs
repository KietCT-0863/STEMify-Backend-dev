using Contracts.Domains;

namespace Product.Domain.Entities
{
    public class KitComponent : EntityBase<int>
    {
        public int KitId { get; set; }
        public int ComponentId { get; set; }
        public int Quantity { get; set; }
        public bool IsMainComponent { get; set; } = false;
        public virtual KitProduct KitProduct { get; set; } = null!;
        public virtual Component Component { get; set; } = null!;
    }
}
