using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Resource.Domain.Entities
{
    public class Topic : EntityBase<int>
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<LessonTopic> LessonTopics { get; set; } = [];
    }
}
