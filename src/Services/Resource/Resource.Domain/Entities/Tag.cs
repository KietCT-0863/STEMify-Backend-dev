using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class Tag : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<LessonAssetTag> LessonAssetTags { get; set; } = [];
    }
}
