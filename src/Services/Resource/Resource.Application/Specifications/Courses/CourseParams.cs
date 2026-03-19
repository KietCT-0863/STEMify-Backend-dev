using Resource.Domain.Enums;
using Shared.SeedWork;

namespace Resource.Application.Specifications.Courses
{
    public class CourseParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public CourseStatus? Status { get; set; }
        public string? CreatedByUserId { get; set; }
        public int? AgeRangeId { get; set; }
        public int? CategoryId { get; set; }
        public int? SkillId { get; set; }
        public int? StandardId { get; set; }
        public int? KitId { get; set; }
    }
}
