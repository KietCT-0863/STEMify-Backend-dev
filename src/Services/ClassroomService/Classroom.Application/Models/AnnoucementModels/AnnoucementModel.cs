namespace Classroom.Application.Models.AnnoucementModels
{
    public class AnnoucementModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? FileUrl { get; set; }
    }
}
