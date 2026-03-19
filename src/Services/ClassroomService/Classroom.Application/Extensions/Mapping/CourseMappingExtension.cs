using Classroom.Application.Models.ClassroomModels;

namespace Classroom.Application.Extensions.Mapping
{
    public static class CourseMappingExtension
    {
        public static CourseModel ToCourseModel(this Shared.Protos.Resource.CourseDetail response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response), "CourseDetail cannot be null");
            }
            return new CourseModel
            {
                Id = response.Id,
                Title = response.Title,
                Description = response.Description,
                ImageUrl = response.ImageUrl,
                Duration = response.Duration,
                AgeRangeLabel = response.AgeRangeLabel,
                Code = response.Code,
                QuizIds = response.QuizIds,
                Lessons = response.Lessons.Select(lesson => new LessonModel
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    SectionIds = lesson.SectionIds,
                }).ToList()
            };
        }
    }
}
