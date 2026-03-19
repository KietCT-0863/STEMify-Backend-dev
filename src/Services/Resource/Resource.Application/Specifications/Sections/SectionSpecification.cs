using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Sections
{
    public class SectionByIdSpecification : Specification<Section>
    {
        public SectionByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.Contents)
                .ThenInclude(c => c.Quiz);
        }
    }

    public class SectionWithIncludesSpecification : Specification<Section>
    {
        public SectionWithIncludesSpecification()
        {
            Query.Include(c => c.Contents).ThenInclude(c => c.Quiz);
        }
    }

    public class PublishedSectionByLessonIdSpecification : Specification<Section>
    {
        public PublishedSectionByLessonIdSpecification(int lessonId)
        {
            Query.Where(c => c.LessonId == lessonId && c.Status == Domain.Enums.SectionStatus.Published);
        }
    }
}
