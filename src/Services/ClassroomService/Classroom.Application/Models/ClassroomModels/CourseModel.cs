namespace Classroom.Application.Models.ClassroomModels
{
    public class CourseModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; } = 0;
        public string AgeRangeLabel { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public IReadOnlyList<LessonModel> Lessons { get; set; } = [];
        public IReadOnlyList<int> QuizIds { get; set; } = [];
    }
}
