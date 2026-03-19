using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Lesson : EntityAuditBase<int>
    {
        [Required, StringLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string LearningOutcome { get; set; } = string.Empty;
        public string? Requirement { get; set; }

        [Required]
        public int Duration { get; set; } = 0;

        [Required]
        public int OrderIndex { get; set; }
        public LessonStatus Status { get; set; } = LessonStatus.Draft;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey("Course")]
        public int CourseId { get; set; }

        // Navigation properties
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
        public virtual ICollection<LessonSkill> LessonSkills { get; set; } =
            new List<LessonSkill>();
        public virtual ICollection<LessonTopic> LessonTopics { get; set; } =
            new List<LessonTopic>();
        public virtual ICollection<LessonStandard> LessonStandards { get; set; } =
            new List<LessonStandard>();
        public virtual ICollection<LessonAsset> LessonAssets { get; set; } =
            new List<LessonAsset>();
        public virtual Course Course { get; set; }
    }
}
