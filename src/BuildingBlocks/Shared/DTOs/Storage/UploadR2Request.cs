namespace Shared.DTOs.Storage
{
    public class UploadR2Request
    {
        public byte[] FileBytes { get; set; } = [];
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string? Folder { get; set; }
    }
}
