using Order.Domain.Enums;

namespace Order.Application.Models
{
    public class OrganizationSubscriptionOrderDto
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public int PlanBillingCycleId { get; set; }
        public string PlanBillingCycle { get; set; } = String.Empty;
        public int ContractId { get; set; }
        public int? ParentSubscriptionId { get; set; }
        public string PlanName { get; set; } = String.Empty;
        public string Code { get; set; } = String.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public OrganizationSubscriptionOrderStatus Status { get; set; } = OrganizationSubscriptionOrderStatus.Pending;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxStudentSeats { get; set; }
        public int MaxTeacherSeats { get; set; }
        public int CurrentStudentSeats { get; set; }
        public int CurrentTeacherSeats { get; set; }
        public int CurriculumCount { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
    }

    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int PlanBillingCycleId { get; set; }
        public string PlanName { get; set; } = String.Empty;
        public string Code { get; set; } = String.Empty;
        public string PlanBillingCycle { get; set; } = String.Empty;
        public decimal GrossAmount { get; set; }
        public decimal NetAmount { get; set; }
        public OrganizationSubscriptionOrderStatus Status { get; set; } = OrganizationSubscriptionOrderStatus.Pending;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxStudentSeats { get; set; }
        public int MaxTeacherSeats { get; set; }
        public int CurrentStudentSeats { get; set; }
        public int CurrentTeacherSeats { get; set; }
    }
}
