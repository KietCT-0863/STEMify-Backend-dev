using Ardalis.Specification;
using Product.Domain.Entities;

namespace Product.Application.Specifications
{
    public class PlanByIdSpecification : Specification<Plan>
    {
        public PlanByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.PlanCurriculums)
                .Include(c => c.PlanBillingCycles);
        }
    }

    public class PlanWithIncludesSpecification : Specification<Plan>
    {
        public PlanWithIncludesSpecification()
        {
            Query.Include(x => x.PlanCurriculums);
        }
    }
}
