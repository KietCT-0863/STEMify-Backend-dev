using Shared.SeedWork;

namespace Resource.Application.Specifications.ProgramLearningOutcomes
{
    public class ProgramLearningOutcomeParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? CurriculumId { get; set; }
    }
}
