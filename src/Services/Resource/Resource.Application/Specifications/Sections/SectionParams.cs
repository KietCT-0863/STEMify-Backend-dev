using Resource.Domain.Enums;
using Shared.SeedWork;

namespace Resource.Application.Specifications.Sections
{
    public class SectionParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public SectionStatus? Status { get; set; }
        public int? LessonId { get; set; }
    }
}
