using Contracts.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Quiz : EntityBase<int>
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double TotalMarks { get; set; } = 100;
        public double PassingMarks { get; set; } = 80;
        public int DurationDays { get; set; }
        public int? TimeLimitInMinutes { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }

        [ForeignKey("Content")]
        public int ContentId { get; set; }

        // Navigation properties
        public virtual Content Content { get; set; } = null!;
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
