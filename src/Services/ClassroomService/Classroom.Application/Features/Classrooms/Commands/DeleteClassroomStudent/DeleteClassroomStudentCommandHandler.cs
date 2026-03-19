using Classroom.Application.Common.Interfaces;
using MediatR;

namespace Classroom.Application.Features.Classrooms.Commands.DeleteClassroomStudent
{
    public class DeleteClassroomStudentCommandHandler : IRequestHandler<DeleteClassroomStudentCommand, Unit>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        public DeleteClassroomStudentCommandHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<Unit> Handle(DeleteClassroomStudentCommand request, CancellationToken cancellationToken)
        {
            var classroomStudents = await _unitOfWork.ClassroomStudents
                .FindAsync(c => c.ClassroomId == request.ClassroomId && request.StudentIds.Contains(c.StudentId), cancellationToken);

            await _unitOfWork.ClassroomStudents.DeleteRangeAsync(classroomStudents, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
