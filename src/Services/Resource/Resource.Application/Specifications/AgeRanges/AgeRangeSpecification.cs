using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.AgeRanges
{
    public class AgeRangeByIdSpecification : Specification<AgeRange>
    {
        public AgeRangeByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.Courses);
        }
    }

    public class AgeRangeWithIncludesSpecification : Specification<AgeRange>
    {
        public AgeRangeWithIncludesSpecification()
        {
            Query.Include(x => x.Courses);
        }
    }
}
