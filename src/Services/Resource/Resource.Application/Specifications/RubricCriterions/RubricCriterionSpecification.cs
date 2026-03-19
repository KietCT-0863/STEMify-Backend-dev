using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.RubricCriterions
{
    public class RubricCriterionByIdSpecification : Specification<RubricCriterion>
    {
        public RubricCriterionByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.AssignmentQuestion);
        }
    }

    public class RubricCriterionWithIncludesSpecification : Specification<RubricCriterion>
    {
        public RubricCriterionWithIncludesSpecification()
        {
            Query.Include(x => x.AssignmentQuestion);
        }
    }
}
