namespace Classroom.Application.Models.ProgressModels
{
    public class StudentLessonProgressModel
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
