using MediatR;

namespace Classroom.Application.Features.Classrooms.Commands.DeleteClassroomStudent
{
    public class DeleteClassroomStudentCommand : IRequest<Unit>
    {
        public int ClassroomId { get; set; }
        public List<string> StudentIds { get; set; } = [];
    }
}
