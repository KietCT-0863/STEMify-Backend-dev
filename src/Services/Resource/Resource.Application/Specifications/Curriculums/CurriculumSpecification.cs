using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Curriculums
{
    public class CurriculumByIdSpecification : Specification<Curriculum>
    {
        public CurriculumByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id)
                .Include(c => c.ProgramLearningOutcomes)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course)
                .Include(c => c.CurriculumEmulations)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course)
                        .ThenInclude(cc => cc.AgeRange)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course)
                        .ThenInclude(cc => cc.Lessons)
                            .ThenInclude(l => l.LessonSkills)
                                .ThenInclude(ls => ls.Skill)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course)
                        .ThenInclude(cc => cc.Lessons)
                            .ThenInclude(l => l.LessonTopics)
                                .ThenInclude(la => la.Topic)
                .AsSplitQuery()
                ;
        }
    }

    public class CurriculumWithIncludesSpecification : Specification<Curriculum>
    {
        public CurriculumWithIncludesSpecification()
        {
            Query
                .Include(c => c.ProgramLearningOutcomes)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course);
        }
    }

    public class CurriculumBasicSnapshotSpecification : Specification<Curriculum>
    {
        public CurriculumBasicSnapshotSpecification(int curriculumId)
        {
            Query
                .Where(c => c.Id == curriculumId)
                .Include(c => c.CurriculumCourses)
                    .ThenInclude(cc => cc.Course)
                .Include(c => c.CurriculumEmulations)
                .AsSplitQuery();
        }
    }
}
