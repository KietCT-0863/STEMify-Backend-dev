using Shared.SeedWork;

namespace Resource.Application.Specifications.Answers
{
    public class AnswerParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? QuestionId { get; set; }
    }
}
