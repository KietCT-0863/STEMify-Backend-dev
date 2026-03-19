using Contracts.Domains;

namespace Resource.Domain.Entities
{
    public class LessonAssetTag : EntityBase<int>
    {
        public int LessonAssetId { get; set; }
        public int TagId { get; set; }
        public LessonAsset LessonAsset { get; set; }
        public Tag Tag { get; set; }
    }
}
