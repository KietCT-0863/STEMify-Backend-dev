using Contracts.Domains;

namespace Order.Domain.Entities
{
    public class OrganizationType : EntityBase<int>
    {
        public string Name { get; set; } = null!;

        // Navigation property
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}
