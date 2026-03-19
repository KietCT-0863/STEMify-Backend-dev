namespace Resource.Application.Models.Lesson
{
    public class CreateLessonAssetRequest
    {
        public int LessonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] AssetBytes { get; set; }
    }
}
