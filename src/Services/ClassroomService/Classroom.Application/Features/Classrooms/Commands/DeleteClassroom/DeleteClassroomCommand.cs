using MediatR;

namespace Classroom.Application.Features.Classrooms.Commands.DeleteClassroom
{
    public class DeleteClassroomCommand : IRequest<bool>
    {
        public int ClassroomId { get; set; }

        public DeleteClassroomCommand(int classroomId)
        {
            ClassroomId = classroomId;
        }
    }
}
