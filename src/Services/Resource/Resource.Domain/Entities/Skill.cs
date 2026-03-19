using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Resource.Domain.Entities
{
    public class Skill : EntityBase<int>
    {
        [Required]
        [StringLength(100)]
        public string SkillName { get; set; }
        public virtual ICollection<LessonSkill> LessonSkills { get; set; }
    }
}
