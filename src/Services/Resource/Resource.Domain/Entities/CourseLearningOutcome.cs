using Contracts.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class CourseLearningOutcome : EntityBase<int>
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("Course")]
        public int CourseId { get; set; }

        // Navigation properties
        public virtual Course Course { get; set; }
        public virtual ICollection<LearningOutcomeMapping> LearningOutcomeMappings { get; set; } = new List<LearningOutcomeMapping>();
    }
}
