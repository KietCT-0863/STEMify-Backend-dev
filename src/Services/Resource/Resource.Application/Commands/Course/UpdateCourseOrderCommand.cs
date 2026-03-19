using MediatR;

namespace Resource.Application.Commands.Course
{
    public class UpdateCoursesOrderCommand : IRequest<Unit>
    {
        public int CurriculumId { get; set; }
        public IReadOnlyList<int> OrderedCourseIds { get; set; } = new List<int>();
    }
}
