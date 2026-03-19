using Resource.Domain.Enums;
using Shared.SeedWork;

namespace Resource.Application.Specifications.Lessons
{
    public class LessonParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public LessonStatus? Status { get; set; }
        public string? CreatedByUserId { get; set; }
        public int? CourseId { get; set; }
        public int? Duration { get; set; }
        public int? AgeRangeId { get; set; }
        public int? TopicId { get; set; }
        public int? SkillId { get; set; }
        public int? StandardId { get; set; }
    }
}
