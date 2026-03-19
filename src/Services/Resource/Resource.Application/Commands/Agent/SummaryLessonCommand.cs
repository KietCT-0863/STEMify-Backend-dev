using MediatR;

namespace Resource.Application.Commands.Agent
{
    public class SummaryLessonCommand : IRequest<IAsyncEnumerable<string>>
    {
        public int LessonId { get; set; }
    }
}
