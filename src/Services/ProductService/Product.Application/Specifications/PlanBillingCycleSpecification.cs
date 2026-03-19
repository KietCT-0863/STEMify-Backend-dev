using Ardalis.Specification;
using Product.Domain.Entities;

namespace Product.Application.Specifications
{
    public class PlanBillingCycleByIdSpecification : Specification<PlanBillingCycle>
    {
        public PlanBillingCycleByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Plan)
                .Include(c => c.ParentPlanBillingCycle)
                .Include(c => c.AddOnBillingCycles);
        }
    }

    public class PlanBillingCycleWithIncludesSpecification : Specification<PlanBillingCycle>
    {
        public PlanBillingCycleWithIncludesSpecification()
        {
            Query
                .Include(c => c.ParentPlanBillingCycle)
                .Include(c => c.AddOnBillingCycles);
        }
    }
}
