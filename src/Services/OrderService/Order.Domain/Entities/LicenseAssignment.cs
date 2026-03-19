using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class LicenseAssignment : EntityBase<int>
    {
        public int OrganizationSubscriptionOrderId { get; set; }
        // This is OrganizationUser.Id, not User.Id
        public string OrganizationUserId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public LicenseAssignmentStatus Status { get; set; } = LicenseAssignmentStatus.Pending;
        public LicenseType LicenseType { get; set; }

        // Navigation property
        public OrganizationSubscriptionOrder OrganizationSubscriptionOrder { get; set; }
    }
}
