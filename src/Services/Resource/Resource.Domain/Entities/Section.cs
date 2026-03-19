using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Section : EntityBase<int>
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }

        [Required]
        public int OrderIndex { get; set; }
        public SectionStatus Status { get; set; } = SectionStatus.Draft;

        [ForeignKey("Lesson")]
        public int LessonId { get; set; }
        public bool IsVisibleToStudent { get; set; } = true;

        // Navigation properties
        public virtual Lesson Lesson { get; set; } = null!;
        public virtual ICollection<Content> Contents { get; set; } = new List<Content>();
    }
}
