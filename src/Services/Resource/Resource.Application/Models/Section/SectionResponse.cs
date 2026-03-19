using Resource.Domain.Enums;

namespace Resource.Application.Models.Section
{
    public class SectionResponse
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int OrderIndex { get; set; }
        public SectionStatus Status { get; set; }
        public int LessonId { get; set; }
        public bool IsVisibleToStudent { get; set; }

        public List<int> ContentIds { get; set; } = new List<int>();
        public List<int> QuizIds { get; set; } = new List<int>();
    }
}
