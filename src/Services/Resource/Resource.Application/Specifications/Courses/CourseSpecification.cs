using Ardalis.Specification;
using Resource.Domain.Entities;

namespace Resource.Application.Specifications.Courses
{
    public class CourseByIdSpecification : Specification<Course>
    {
        public CourseByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id)
                .Include(c => c.AgeRange)
                .Include(c => c.Lessons)
                .ThenInclude(l => l.LessonSkills)
                .ThenInclude(ls => ls.Skill)
                .Include(c => c.Lessons)
                .ThenInclude(l => l.LessonStandards)
                .ThenInclude(ls => ls.Standard)
                .Include(c => c.Lessons)
                .ThenInclude(l => l.LessonTopics)
                .ThenInclude(st => st.Topic)
                .Include(st => st.CurriculumCourses)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Sections)
                    .ThenInclude(s => s.Contents)
                    .ThenInclude(c => c.Quiz);
        }
    }

    public class CourseDetailByIdSpecification : Specification<Course>
    {
        public CourseDetailByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id)
                .Include(c => c.CourseLearningOutcomes)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.LessonStandards)
                        .ThenInclude(ls => ls.Standard)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.LessonTopics)
                        .ThenInclude(st => st.Topic)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.LessonSkills)
                        .ThenInclude(st => st.Skill)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Sections)
                        .ThenInclude(st => st.Contents)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.LessonAssets)
                ;
        }
    }

    public class CourseWithIncludesSpecification : Specification<Course>
    {
        public CourseWithIncludesSpecification()
        {
            Query.Include(c => c.AgeRange);
        }
    }
}
