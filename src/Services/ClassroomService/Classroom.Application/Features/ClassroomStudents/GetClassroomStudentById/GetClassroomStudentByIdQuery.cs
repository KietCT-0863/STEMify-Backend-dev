using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.ClassroomStudents.GetClassroomStudentById
{
    public class GetClassroomStudentByIdQuery : IRequest<GrpcClassroomStudentResponse>
    {
        public int ClassroomId { get; set; }
        public string StudentId { get; set; }
    }
}
