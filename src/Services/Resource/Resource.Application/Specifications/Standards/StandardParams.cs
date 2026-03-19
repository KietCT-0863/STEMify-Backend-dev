using Shared.SeedWork;

namespace Resource.Application.Specifications.Standards
{
    public class StandardParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
    }
}
