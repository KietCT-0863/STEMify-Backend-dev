using Classroom.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignment
{
    public class CreateStudentAssignmentCommandHandler : IRequestHandler<CreateStudentAssignmentCommand>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<CreateStudentAssignmentCommandHandler> _logger;

        public CreateStudentAssignmentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<CreateStudentAssignmentCommandHandler> logger)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(CreateStudentAssignmentCommand request, CancellationToken cancellationToken)
        {
            var studentAssignment = new Domain.Entities.StudentAssignment
            {
                StudentId = request.StudentId,
                AssignmentId = request.AssignmentId,
                AssignedAt = DateTime.UtcNow,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                DueDate = request.DueDate,
                AttemptCount = 0,
                Status = Domain.Enums.StudentAssignmentStatus.Assigned,
                StudentSectionProgressId = request.StudentSectionProgressId,
            };
            await _unitOfWork.StudentAssignments.AddAsync(studentAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created StudentAssignment with ID {StudentAssignmentId} for Student {StudentId}", studentAssignment.Id, studentAssignment.StudentId);
        }
    }
}
