using Contracts.Domains;
using Resource.Domain.Enums;

namespace Resource.Domain.Entities
{
    public class AssignmentQuestion : EntityBase<int>
    {
        public int AssignmentId { get; set; }
        public AssignmentQuestionType Type { get; set; } = AssignmentQuestionType.Text; // Text, FileUpload
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public decimal Points { get; set; }

        // Navigation properties
        public virtual Assignment Assignment { get; set; }
        public virtual ICollection<RubricCriterion> RubricCriterions { get; set; } = new List<RubricCriterion>();
    }
}
