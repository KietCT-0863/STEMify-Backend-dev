using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Assignments
{
    public class AssignmentByIdSpecification : Specification<Assignment>
    {
        public AssignmentByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.AssignmentQuestions)
                                            .ThenInclude(q => q.RubricCriterions);
        }
    }

    public class AssignmentWithIncludesSpecification : Specification<Assignment>
    {
        public AssignmentWithIncludesSpecification()
        {
            Query.Include(x => x.AssignmentQuestions);
        }
    }

    public class QuestionByAssignmentIdSpecification : Specification<AssignmentQuestion>
    {
        public QuestionByAssignmentIdSpecification(int assignmentId)
        {
            Query.Where(x => x.AssignmentId == assignmentId);
        }
    }
}
