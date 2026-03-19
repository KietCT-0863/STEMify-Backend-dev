using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Resource.Domain.Entities
{
    public class Curriculum : EntityAuditBase<int>
    {
        [StringLength(255)]
        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public CurriculumStatus Status { get; set; } = CurriculumStatus.Draft;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAt { get; set; }

        //[ForeignKey("AgeRange")]
        //public int AgeRangeId { get; set; }

        //// Navigation properties
        //public virtual AgeRange AgeRange { get; set; }
        public virtual ICollection<CurriculumCourse> CurriculumCourses { get; set; } = new List<CurriculumCourse>();
        public virtual ICollection<CurriculumEmulation> CurriculumEmulations { get; set; } = new List<CurriculumEmulation>();
        public virtual ICollection<ProgramLearningOutcome> ProgramLearningOutcomes { get; set; } = new List<ProgramLearningOutcome>();
    }
}
