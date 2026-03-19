using MediatR;

namespace Classroom.Application.Features.Classrooms.Commands.CreateClassroomStudent
{
    public class CreateClassroomStudentCommand : IRequest<Unit>
    {
        public int ClassroomId { get; set; }
        public List<string>? StudentIds { get; set; }
        public List<string>? StudentEmails { get; set; }
    }
}
