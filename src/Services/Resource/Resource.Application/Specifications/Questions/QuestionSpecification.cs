using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Questions
{
    public class QuestionByIdSpecification : Specification<Question>
    {
        public QuestionByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.Answers);
        }
    }

    public class QuestionByQuizIdSpecification : Specification<Question>
    {
        public QuestionByQuizIdSpecification(int quizId)
        {
            Query.Where(x => x.QuizId == quizId)
                .Include(x => x.Answers);
        }
    }
}
