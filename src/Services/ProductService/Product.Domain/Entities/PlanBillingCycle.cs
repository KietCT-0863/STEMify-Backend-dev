using Contracts.Domains;
using Product.Domain.Enums;

namespace Product.Domain.Entities
{
    public class PlanBillingCycle : EntityBase<int>
    {
        public int PlanId { get; set; }
        public BillingCycle BillingCycle { get; set; } = BillingCycle.Annual;
        public decimal Price { get; set; }
        public int? MaxStudentSeats { get; set; }
        public int? MaxTeacherSeats { get; set; }
        public bool IsAddOn { get; set; }
        public int? ParentPlanBillingCycleId { get; set; }

        // Navigation Properties
        public virtual Plan? Plan { get; set; }
        public virtual PlanBillingCycle? ParentPlanBillingCycle { get; set; }
        public virtual ICollection<PlanBillingCycle> AddOnBillingCycles { get; set; } = new List<PlanBillingCycle>();
    }
}
