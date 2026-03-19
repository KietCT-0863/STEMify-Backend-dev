using Contracts.Domains;
using Order.Domain.Enums;

namespace Order.Domain.Entities
{
    public class OrganizationSubscriptionOrder : EntityAuditBase<int>
    {
        public int OrganizationId { get; set; }
        public int PlanBillingCycleId { get; set; }
        public int ContractId { get; set; }
        public int? ParentSubscriptionId { get; set; }
        public string Code { get; set; } = null!;
        public string PlanName { get; set; } = String.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public OrganizationSubscriptionOrderStatus Status { get; set; } = OrganizationSubscriptionOrderStatus.Pending;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxStudentSeats { get; set; }
        public int MaxTeacherSeats { get; set; }
        public int CurriculumCount { get; set; }

        // Navigation properties
        public Organization Organization { get; set; }
        public Contract Contract { get; set; }
        public OrganizationSubscriptionOrder ParentSubscription { get; set; }
        public ICollection<OrganizationSubscriptionOrder> ChildSubscriptions { get; set; } = new List<OrganizationSubscriptionOrder>();
        public ICollection<SubscriptionOrderCurriculum> SubscriptionOrderCurriculums { get; set; } = new List<SubscriptionOrderCurriculum>();
        public ICollection<LicenseAssignment> LicenseAssignments { get; set; } = new List<LicenseAssignment>();
    }
}
