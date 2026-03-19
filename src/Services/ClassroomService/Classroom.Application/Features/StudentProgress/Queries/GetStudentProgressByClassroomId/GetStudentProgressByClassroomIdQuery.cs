using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentProgress.Queries.GetStudentProgressByClassroomId
{
    public class GetStudentProgressByClassroomIdQuery : IRequest<GrpcClassroomStudentProgressResponse>
    {
        public int ClassroomId { get; set; }
        public int CourseId { get; set; }
    }
}
