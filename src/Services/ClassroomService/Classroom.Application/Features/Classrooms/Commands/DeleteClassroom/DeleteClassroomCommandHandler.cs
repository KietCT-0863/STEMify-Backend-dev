using Classroom.Application.Common.Interfaces;
using Classroom.Domain.Enums;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.Classrooms.Commands.DeleteClassroom
{
    public class DeleteClassroomCommandHandler : IRequestHandler<DeleteClassroomCommand, bool>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;

        public DeleteClassroomCommandHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(
            DeleteClassroomCommand request,
            CancellationToken cancellationToken
        )
        {
            var classroom = await _unitOfWork.Classrooms.FindByIdAsync(
                request.ClassroomId,
                cancellationToken
            );
            if (classroom == null)
            {
                throw new NotFoundException($"Classroom with ID {request.ClassroomId} not found.");
            }

            classroom.Status = ClassroomStatus.Deleted;
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0;
        }
    }
}
