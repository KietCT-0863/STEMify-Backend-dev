using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class Organization : EntityAuditBase<int>
    {
        public int OrganizationTypeId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public OrganizationStatus Status { get; set; } = OrganizationStatus.Active;

        // Navigation properties
        public OrganizationType OrganizationType { get; set; }
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<OrganizationSubscriptionOrder> SubscriptionOrders { get; set; } = new List<OrganizationSubscriptionOrder>();
    }
}
