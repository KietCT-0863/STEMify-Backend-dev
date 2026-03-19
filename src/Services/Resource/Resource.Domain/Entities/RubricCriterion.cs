using Contracts.Domains;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class RubricCriterion : EntityBase<int>
    {
        [Required]
        [StringLength(255)]
        public string CriterionName { get; set; }
        public string? Description { get; set; }
        [ForeignKey("AssignmentQuestion")]
        public int AssignmentQuestionId { get; set; }
        public decimal MaxPoints { get; set; } = 100;

        public virtual AssignmentQuestion AssignmentQuestion { get; set; }
    }
}
