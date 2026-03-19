using Shared.SeedWork;

namespace Resource.Application.Specifications.CourseLearningOutcomes
{
    public class CourseLearningOutcomeParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? CourseId { get; set; }
    }
}
