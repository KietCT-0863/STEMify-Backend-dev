using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Skills
{
    public class SkillByIdSpecification : Specification<Skill>
    {
        public SkillByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id).Include(c => c.LessonSkills);
        }
    }

    public class SkillWithIncludesSpecification : Specification<Skill>
    {
        public SkillWithIncludesSpecification()
        {
            Query.Include(x => x.LessonSkills);
        }
    }
}
