using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Lesson
{
    public class QueryLessonsQuery : IRequest<PagedLessonList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public Shared.Enums.SortDirection? SortDirection { get; set; }
        public int? CourseId { get; set; }
        public string? CreatedByUserId { get; set; }
        public Resource.Domain.Enums.LessonStatus? Status { get; set; }
        public int? Duration { get; set; }
        public int? AgeRangeId { get; set; }
        public int? TopicId { get; set; }
        public int? SkillId { get; set; }
        public int? StandardId { get; set; }
    }
}
