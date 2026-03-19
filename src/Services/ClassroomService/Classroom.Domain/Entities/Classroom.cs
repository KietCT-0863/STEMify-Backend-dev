using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class Classroom : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public Guid TeacherId { get; set; }
        public string ClassCode { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public ClassroomStatus Status { get; set; } = ClassroomStatus.Pending;
        public int CourseId { get; set; }
        public int OrganizationSubscriptionOrderId { get; set; }
        public int OrganizationId { get; set; }
        public virtual ICollection<Annoucement> Annoucements { get; set; } = [];
        public virtual ICollection<ClassroomStudent> ClassroomStudents { get; set; } = [];
        public virtual ICollection<CurriculumEnrollment> CurriculumEnrollments { get; set; } = [];
    }
}
