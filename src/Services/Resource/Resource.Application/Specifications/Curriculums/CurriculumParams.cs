using Resource.Domain.Enums;
using Shared.SeedWork;

namespace Resource.Application.Specifications.Curriculums
{
    public class CurriculumParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public CurriculumStatus? Status { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? Code { get; set; }
    }
}
