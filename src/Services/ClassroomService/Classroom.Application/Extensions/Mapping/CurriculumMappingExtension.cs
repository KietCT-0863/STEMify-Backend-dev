using Classroom.Application.Models.ClassroomModels;
using Shared.Protos.Resource;

namespace Classroom.Application.Extensions.Mapping
{
    public static class CurriculumMappingExtension
    {
        public static CurriculumModel ToCurriculumModel(this CurriculumDetails response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response), "CurriculumDetail cannot be null");
            }
            return new CurriculumModel
            {
                Id = response.Id,
                Title = response.Title,
                Description = response.Description,
                ImageUrl = response.ImageUrl,
                Code = response.Code,
                Duration = response.Duration,
                Courses = response.Courses.Select(c => new Models.ClassroomModels.CourseDetail
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Code = c.Code,
                    OrderIndex = c.CourseOrderIndex,
                    ImageUrl = c.ImageUrl,
                    Status = c.Status,
                    Duration = c.Duration,
                    Lessons = c.Lessons.Select(l => new LessonModel
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Duration = l.Duration,
                    }).ToList(),
                }).ToList(),
            };
        }
    }
}
