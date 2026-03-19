using Contracts.Domains;
using Product.Domain.Enums;

namespace Product.Domain.Entities
{
    public class Plan : EntityAuditBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public PlanStatus Status = PlanStatus.Draft;
        public string? Description { get; set; }
        public string? AccessSupportDetail { get; set; }
        public int CurriculumCount { get; set; } = 0;
        public int? MaxStudentSeats { get; set; }
        public int? MaxTeacherSeats { get; set; }

        // Navigation Properties
        public ICollection<PlanCurriculum> PlanCurriculums { get; set; } = new List<PlanCurriculum>();
        public ICollection<PlanBillingCycle> PlanBillingCycles { get; set; } = new List<PlanBillingCycle>();
    }
}
