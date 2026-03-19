namespace Classroom.Application.Models.ClassroomModels
{
    public class CourseStatsData
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public List<double> QuizScores { get; set; } = new();
        public List<double> AssignmentScores { get; set; } = new();
    }
}
