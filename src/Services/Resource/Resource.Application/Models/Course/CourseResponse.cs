using Resource.Domain.Enums;

namespace Resource.Application.Models.Course
{
    public class CourseResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string StudentTasks { get; set; } = string.Empty;
        public string? Prerequisites { get; set; }
        public int Duration { get; set; }
        public CourseStatus Status { get; set; }
        public CourseLevel Level { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? ReviewedByUserId { get; set; }
        public int AgeRangeId { get; set; }
        public int? KitId { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string AgeRangeLabel { get; set; } = string.Empty;
        public List<int> LessonIds { get; set; } = new List<int>();
        public List<string> TopicNames { get; set; } = new List<string>();
        public List<string> SkillNames { get; set; } = new List<string>();
        public List<string> StandardNames { get; set; } = new List<string>();
    }
}
