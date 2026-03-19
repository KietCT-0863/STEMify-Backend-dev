using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Standards
{
    public class StandardByIdSpecification : Specification<Standard>
    {
        public StandardByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.LessonStandards);
        }
    }

    public class StandardWithIncludesSpecification : Specification<Standard>
    {
        public StandardWithIncludesSpecification()
        {
            Query.Include(x => x.LessonStandards);
        }
    }
}
