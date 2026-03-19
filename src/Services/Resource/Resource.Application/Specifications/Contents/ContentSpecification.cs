using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Contents
{
    public class ContentByIdSpecification : Specification<Content>
    {
        public ContentByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Quiz)
                .Include(c => c.Assignment);
        }
    }

    public class ContentWithIncludesSpecification : Specification<Content>
    {
        public ContentWithIncludesSpecification()
        {
            Query.Include(c => c.Quiz)
                .Include(c => c.Assignment);
        }
    }
}
