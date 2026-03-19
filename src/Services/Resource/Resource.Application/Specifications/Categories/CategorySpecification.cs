using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Categories
{
    public class CategoryByIdSpecification : Specification<Topic>
    {
        public CategoryByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.LessonTopics);
        }
    }

    public class CategoryWithIncludesSpecification : Specification<Topic>
    {
        public CategoryWithIncludesSpecification()
        {
            Query.Include(x => x.LessonTopics);
        }
    }
}
