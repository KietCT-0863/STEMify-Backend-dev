using Resource.Domain.Enums;

namespace Resource.Application.Models.Curriculum
{
    public class CurriculumResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public CurriculumStatus Status { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? ApprovedByUserId { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int CourseCount { get; set; }
        public decimal Price { get; set; }
    }
}
