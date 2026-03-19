using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Quizzes
{
    public class QuizByIdSpecification : Specification<Quiz>
    {
        public QuizByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Content)
                .Include(c => c.Questions)
                    .ThenInclude(q => q.Answers);
        }
    }

    public class QuizWithIncludesSpecification : Specification<Quiz>
    {
        public QuizWithIncludesSpecification()
        {
            Query.Include(x => x.Questions);
        }
    }
}
