using Contracts.Domains;
using Resource.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resource.Domain.Entities
{
    public class Content : EntityBase<int>
    {
        public ContentType ContentType { get; set; } = ContentType.Text;

        [Required]
        public string ContentBody { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public string? FileUrl { get; set; }
        public DateTimeOffset? UploadDate { get; set; }
        public ContentStatus Status { get; set; } = ContentStatus.Published;

        [ForeignKey("Section")]
        public int SectionId { get; set; }

        // Navigation properties
        public virtual Section Section { get; set; } = null!;
        public virtual Quiz? Quiz { get; set; }
        public virtual Assignment? Assignment { get; set; }
    }
}
