namespace Shared.DTOs.Storage
{
    public class UploadR2Response
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileKey { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}
