using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class Contract : EntityAuditBase<int>
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public ContractStatus Status { get; set; } = ContractStatus.Active;
        public string? FileUrl { get; set; }
        public string? Description { get; set; }

        // Navigation properties
        public Organization Organization { get; set; }
        public ICollection<OrganizationSubscriptionOrder> SubscriptionOrders { get; set; } = new List<OrganizationSubscriptionOrder>();
    }
}
