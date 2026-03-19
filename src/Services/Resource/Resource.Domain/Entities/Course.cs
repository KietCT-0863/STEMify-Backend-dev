using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Course : EntityAuditBase<int>
    {
        [StringLength(255)]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        [StringLength(255)]
        [Required]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string StudentTasks { get; set; } = string.Empty;
        public string? Prerequisites { get; set; }

        [Required]
        public int Duration { get; set; } = 0;
        public CourseStatus Status { get; set; } = CourseStatus.Draft;
        public CourseLevel Level { get; set; } = CourseLevel.Beginner;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("AgeRange")]
        public int AgeRangeId { get; set; }
        public int? KitId { get; set; }

        // Navigation properties
        public virtual AgeRange AgeRange { get; set; }
        public virtual ICollection<Lesson> Lessons { get; set; } = [];
        public virtual ICollection<CurriculumCourse> CurriculumCourses { get; set; } = new List<CurriculumCourse>();
        public virtual ICollection<CourseLearningOutcome> CourseLearningOutcomes { get; set; } = new List<CourseLearningOutcome>();
    }
}
