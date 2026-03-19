using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class LessonAsset : EntityBase<int>
    {
        public int LessonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long Size { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<LessonAssetTag> LessonAssetTags { get; set; } = [];
        public virtual Lesson Lesson { get; set; }
    }
}
