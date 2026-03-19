using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Resource.Domain.Entities
{
    public class AgeRange : EntityBase<int>
    {
        [Required]
        [StringLength(100)]
        public string AgeRangeLabel { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }

        public virtual ICollection<Course> Courses { get; set; }
    }
}
