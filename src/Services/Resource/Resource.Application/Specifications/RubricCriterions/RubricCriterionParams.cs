using Shared.SeedWork;

namespace Resource.Application.Specifications.RubricCriterions
{
    public class RubricCriterionParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? AssignmentQuestionId { get; set; }
    }
}
