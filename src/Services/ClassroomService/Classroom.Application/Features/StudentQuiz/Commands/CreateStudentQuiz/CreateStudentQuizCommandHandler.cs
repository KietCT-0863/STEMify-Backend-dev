using Classroom.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuiz
{
    public class CreateStudentQuizCommandHandler : IRequestHandler<CreateStudentQuizCommand, Unit>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<CreateStudentQuizCommandHandler> _logger;
        public CreateStudentQuizCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<CreateStudentQuizCommandHandler> logger)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<Unit> Handle(CreateStudentQuizCommand request, CancellationToken cancellationToken)
        {
            var studentQuiz = new Domain.Entities.StudentQuiz
            {
                StudentId = request.StudentId,
                QuizId = request.QuizId,
                AssignedAt = DateTime.UtcNow,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                TimeLimitMinutes = request.TimeLimitMinutes,
                DueDate = request.DueDate,
                AttemptCount = 0,
                Status = Domain.Enums.StudentQuizStatus.Assigned,
                StudentSectionProgressId = request.StudentSectionProgressId,
            };
            await _unitOfWork.StudentQuizzes.AddAsync(studentQuiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created StudentQuiz with ID {StudentQuizId} for Student {StudentId}", studentQuiz.Id, studentQuiz.StudentId);
            return Unit.Value;
        }
    }
}
