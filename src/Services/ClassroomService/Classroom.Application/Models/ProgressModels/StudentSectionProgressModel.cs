namespace Classroom.Application.Models.ProgressModels
{
    public class StudentSectionProgressModel
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? StudentQuizId { get; set; }
        public int? StudentAssignmentId { get; set; }
    }
}
