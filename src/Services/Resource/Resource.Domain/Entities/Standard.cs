using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Resource.Domain.Entities
{
    public class Standard : EntityBase<int>
    {
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public virtual ICollection<LessonStandard> LessonStandards { get; set; }
    }
}
