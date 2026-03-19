namespace Shared.DTOs.Cloudinary
{
    public class UploadAssetResponse
    {
        public string Type { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public long Size { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? Duration { get; set; }
    }
}
