using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class Annoucement : EntityBase<int>
    {
        public int ClassroomId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? FileUrl { get; set; }

        // Navigation properties
        public virtual Classroom Classroom { get; set; } = null!;
    }
}
