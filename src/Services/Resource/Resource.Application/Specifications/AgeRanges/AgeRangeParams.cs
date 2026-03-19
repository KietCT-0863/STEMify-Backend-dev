using Shared.SeedWork;

namespace Resource.Application.Specifications.AgeRanges
{
    public class AgeRangeParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public int? Age { get; set; }
    }
}
