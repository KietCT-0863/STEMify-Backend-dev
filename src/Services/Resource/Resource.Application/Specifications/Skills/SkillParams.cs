using Shared.SeedWork;

namespace Resource.Application.Specifications.Skills
{
    public class SkillParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
    }
}
