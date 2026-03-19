using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Course
{
    public class QueryCoursesQuery : IRequest<PagedCourseList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public Shared.Enums.SortDirection? SortDirection { get; set; }
        public string? CreatedByUserId { get; set; }
        public Resource.Domain.Enums.CourseStatus? Status { get; set; }
        public int? AgeRangeId { get; set; }
        public int? CategoryId { get; set; }
        public int? SkillId { get; set; }
        public int? StandardId { get; set; }
        public int? KitId { get; set; }
    }
}
