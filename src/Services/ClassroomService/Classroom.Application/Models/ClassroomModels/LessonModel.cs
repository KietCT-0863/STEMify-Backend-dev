namespace Classroom.Application.Models.ClassroomModels
{
    public class LessonModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int OrderIndex { get; set; }
        public IReadOnlyList<int> SectionIds { get; set; } = [];
    }
}
