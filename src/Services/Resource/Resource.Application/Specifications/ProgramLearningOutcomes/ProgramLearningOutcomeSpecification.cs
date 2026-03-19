using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.ProgramLearningOutcomes
{
    public class ProgramLearningOutcomeByIdSpecification : Specification<ProgramLearningOutcome>
    {
        public ProgramLearningOutcomeByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.Curriculum);
        }
    }

    public class ProgramLearningOutcomeWithIncludesSpecification : Specification<ProgramLearningOutcome>
    {
        public ProgramLearningOutcomeWithIncludesSpecification()
        {
            Query.Include(x => x.Curriculum);
        }
    }
}
