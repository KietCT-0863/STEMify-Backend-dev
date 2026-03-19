using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class ClassroomStudent : EntityBase<int>
    {
        public string StudentId { get; set; } = string.Empty;
        public int ClassroomId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Classroom Classroom { get; set; } = null!;
    }
}
