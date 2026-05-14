using Ardalis.Specification;
using Resource.Domain.Entities;
using Resource.Domain.Enums;

namespace Resource.Application.Specifications.Lessons
{
    public class LessonByIdSpecification : Specification<Lesson>
    {
        public LessonByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id && c.Status != LessonStatus.Deleted)
                .Include(c => c.Sections)
                .Include(c => c.Course)
                .ThenInclude(c => c.AgeRange)
                .Include(c => c.LessonStandards)
                .ThenInclude(c => c.Standard)
                .Include(c => c.LessonSkills)
                .ThenInclude(c => c.Skill)
                .Include(c => c.LessonTopics)
                .ThenInclude(c => c.Topic);
        }
    }

    public class LessonDetailByIdSpecification : Specification<Lesson>
    {
        public LessonDetailByIdSpecification(int id)
        {
            Query
                .Where(c => c.Id == id)
                .Include(c => c.Course)
                    .ThenInclude(c => c.AgeRange)
                .Include(c => c.LessonStandards)
                    .ThenInclude(c => c.Standard)
                .Include(c => c.LessonSkills)
                    .ThenInclude(c => c.Skill)
                .Include(c => c.LessonTopics)
                    .ThenInclude(c => c.Topic)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Contents)
            ;
        }
    }

    public class LessonWithIncludesSpecification : Specification<Lesson>
    {
        public LessonWithIncludesSpecification()
        {
            Query.Include(c => c.Sections).Include(c => c.Course).ThenInclude(c => c.AgeRange);
        }
    }

    public class LessonsByCourseIdSpecification : Specification<Lesson>
    {
        public LessonsByCourseIdSpecification(int courseId)
        {
            Query.Where(l => l.CourseId == courseId && l.Status != LessonStatus.Deleted);
        }
    }

    public class PublishedLessonsByCourseIdSpecification : Specification<Lesson>
    {
        public PublishedLessonsByCourseIdSpecification(int courseId)
        {
            Query.Where(l => l.CourseId == courseId && l.Status == LessonStatus.Published);
        }
    }
}
