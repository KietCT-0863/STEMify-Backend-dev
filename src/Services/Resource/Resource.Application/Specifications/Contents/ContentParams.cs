using Resource.Domain.Enums;
using Shared.SeedWork;

namespace Resource.Application.Specifications.Contents
{
    public class ContentParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public ContentStatus? Status { get; set; }
        public ContentType? ContentType { get; set; }
        public int? SectionId { get; set; }
    }
}
