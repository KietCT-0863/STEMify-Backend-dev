using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Course
{
    public class GetCourseListQuery : IRequest<CourseList> { }
}
