using Shared.SeedWork;

namespace Resource.Application.Specifications.Questions
{
    public class QuestionParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? QuizId { get; set; }
    }
}
