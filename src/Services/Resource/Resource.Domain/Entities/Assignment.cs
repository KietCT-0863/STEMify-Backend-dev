using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class Assignment : EntityBase<int>
    {
        public int ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal TotalScore { get; set; } = 100;
        public decimal PassingScore { get; set; } = 80;
        public int? DurationDays { get; set; }
        public int? CooldownHours { get; set; }
        public int? MaxAttemptAllowed { get; set; }

        // Navigation properties
        public virtual Content Content { get; set; }
        public virtual ICollection<AssignmentQuestion> AssignmentQuestions { get; set; } = new List<AssignmentQuestion>();
    }
}
