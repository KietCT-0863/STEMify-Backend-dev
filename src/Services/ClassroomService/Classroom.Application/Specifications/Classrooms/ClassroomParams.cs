using Classroom.Domain.Enums;
using Shared.SeedWork;

namespace Classroom.Application.Specifications.Classrooms
{
    public class ClassroomParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public Guid? TeacherId { get; set; }
        public int? CourseId { get; set; }
        public int? OrganizationId { get; set; }
        public int? OrganizationSubscriptionOrderId { get; set; }
        public string? StudentId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
