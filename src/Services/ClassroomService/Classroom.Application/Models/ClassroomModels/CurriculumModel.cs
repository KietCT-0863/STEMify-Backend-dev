namespace Classroom.Application.Models.ClassroomModels
{
    public class CurriculumModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Duration { get; set; }
        public List<CourseDetail> Courses { get; set; } = [];
    }

    public class CourseDetail
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Code { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<LessonModel> Lessons { get; set; } = [];
    }
}
