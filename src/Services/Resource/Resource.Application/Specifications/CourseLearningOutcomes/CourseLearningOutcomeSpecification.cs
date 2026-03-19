using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.CourseLearningOutcomes
{
    public class CourseLearningOutcomeByIdSpecification : Specification<CourseLearningOutcome>
    {
        public CourseLearningOutcomeByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Course)
                .Include(c => c.LearningOutcomeMappings)
                ;
        }
    }

    public class CourseLearningOutcomeWithIncludesSpecification : Specification<CourseLearningOutcome>
    {
        public CourseLearningOutcomeWithIncludesSpecification()
        {
            Query.Include(x => x.Course);
        }
    }
}
