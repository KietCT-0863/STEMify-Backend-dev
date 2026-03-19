using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Lessons
{
    public class LessonAssetByIdSpecification : Specification<LessonAsset>
    {
        public LessonAssetByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id)
                .Include(c => c.LessonAssetTags)
                .ThenInclude(c => c.Tag);
        }
    }
}
