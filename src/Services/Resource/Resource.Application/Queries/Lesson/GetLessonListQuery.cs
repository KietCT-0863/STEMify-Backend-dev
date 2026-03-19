using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Lesson
{
    public class GetLessonListQuery : IRequest<LessonList> { }
}
