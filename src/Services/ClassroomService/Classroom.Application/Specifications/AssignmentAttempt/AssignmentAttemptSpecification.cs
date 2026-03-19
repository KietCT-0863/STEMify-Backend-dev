using Ardalis.Specification;

namespace Classroom.Application.Specifications.AssignmentAttempt
{
    public class GetAssignmentAttemptByIdSpecification : Specification<Domain.Entities.AssignmentAttempt>
    {
        public GetAssignmentAttemptByIdSpecification(int id)
        {
            Query.Where(qa => qa.Id == id)
                 .Include(qa => qa.AssignmentQuestionAttempts)
                     .ThenInclude(sq => sq.RubricScores);
        }
    }
}
