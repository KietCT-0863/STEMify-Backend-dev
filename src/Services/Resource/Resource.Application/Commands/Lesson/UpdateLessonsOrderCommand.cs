using MediatR;

namespace Resource.Application.Commands.Lesson
{
    public class UpdateLessonsOrderCommand : IRequest<Unit>
    {
        public int CourseId { get; set; }
        public IReadOnlyList<int> OrderedLessonIds { get; set; } = new List<int>();
    }
}
