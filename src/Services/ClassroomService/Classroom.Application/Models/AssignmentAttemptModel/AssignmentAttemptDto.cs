using Classroom.Domain.Enums;

namespace Classroom.Application.Models.AssignmentAttemptModel
{
    public class AssignmentAttemptDto
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int StudentAssignmentId { get; set; }
        public AssignmentAttemptStatus Status { get; set; }
        public decimal TotalScore { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int AttemptNumber { get; set; }
    }
}
