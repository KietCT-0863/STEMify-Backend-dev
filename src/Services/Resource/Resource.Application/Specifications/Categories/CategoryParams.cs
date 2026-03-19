using Shared.SeedWork;

namespace Resource.Application.Specifications.Categories
{
    public class CategoryParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
    }
}
