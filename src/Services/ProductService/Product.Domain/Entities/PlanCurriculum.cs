using Contracts.Domains;

namespace Product.Domain.Entities
{
    public class PlanCurriculum : EntityBase<int>
    {
        public int PlanId { get; set; }
        public int CurriculumId { get; set; }

        // Navigation Properties
        public virtual Plan? Plan { get; set; }
    }
}
