using Contracts.Domains;

namespace Product.Domain.Entities
{
    public class Component : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public virtual ICollection<KitComponent> KitComponents { get; set; } = [];
    }
}
