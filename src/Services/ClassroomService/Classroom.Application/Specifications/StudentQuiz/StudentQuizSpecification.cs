using Ardalis.Specification;

namespace Classroom.Application.Specifications.StudentQuiz
{
    public class GetStudentQuizByIdSpecification : Specification<Domain.Entities.StudentQuiz>
    {
        public GetStudentQuizByIdSpecification(int id)
        {
            Query.Where(qa => qa.Id == id)
                 .Include(qa => qa.QuizAttempts)
                     .ThenInclude(sq => sq.QuestionAttempts)
                        .ThenInclude(qqa => qqa.AnswerAttempts);
        }
    }
}
