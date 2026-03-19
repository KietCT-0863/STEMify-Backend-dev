using MediatR;

namespace Resource.Application.Commands.Section
{
    public class UpdateSectionsOrderCommand : IRequest<Unit>
    {
        public int LessonId { get; set; }
        public IReadOnlyList<int> OrderedSectionIds { get; set; } = new List<int>();
    }
}
