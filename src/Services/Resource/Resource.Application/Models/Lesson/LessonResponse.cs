using Resource.Domain.Enums;

namespace Resource.Application.Models.Lesson
{
    public class LessonResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Description { get; set; } = string.Empty;
        public string LearningOutcome { get; set; } = string.Empty;
        public string? Requirement { get; set; }
        public int Duration { get; set; }
        public int OrderIndex { get; set; }
        public LessonStatus Status { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset? LastModifiedDate { get; set; }
        public string AgeRangeLabel { get; set; }

        public List<int> SectionIds { get; set; } = new List<int>();
        public List<string> TopicNames { get; set; } = new List<string>();
        public List<string> SkillNames { get; set; } = new List<string>();
        public List<string> StandardNames { get; set; } = new List<string>();
    }
}
