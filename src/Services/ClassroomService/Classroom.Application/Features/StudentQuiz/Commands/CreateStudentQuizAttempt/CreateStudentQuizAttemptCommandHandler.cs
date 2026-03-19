using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuizAttempt
{
    public class CreateStudentQuizAttemptCommandHandler : IRequestHandler<CreateStudentQuizAttemptCommand, GrpcQuizAttemptResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcQuizClient _quizClient;
        private readonly ILogger<CreateStudentQuizAttemptCommandHandler> _logger;

        public CreateStudentQuizAttemptCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcQuizClient quizClient,
            ILogger<CreateStudentQuizAttemptCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _quizClient = quizClient;
            _logger = logger;
        }

        public async Task<GrpcQuizAttemptResponse> Handle(CreateStudentQuizAttemptCommand request, CancellationToken cancellationToken)
        {
            var studentQuiz = await _unitOfWork.StudentQuizzes.FindByIdAsync(request.StudentQuizId, cancellationToken);
            if (studentQuiz == null)
            {
                _logger.LogWarning("StudentQuiz with Id: {Id} not found", request.StudentQuizId);
                throw new KeyNotFoundException($"StudentQuiz with Id {request.StudentQuizId} not found.");
            }

            // Get the Quiz entity to retrieve CooldownHours
            var quiz = await _quizClient.GetQuizByIdAsync(studentQuiz.QuizId);
            if (quiz == null)
            {
                _logger.LogWarning("Quiz with Id: {Id} not found", studentQuiz.QuizId);
                throw new KeyNotFoundException($"Quiz with Id {studentQuiz.QuizId} not found.");
            }

            var now = DateTime.UtcNow;

            // Check if there is an in-progress attempt still valid
            var ongoingAttempt = await _unitOfWork.QuizAttempts
                .FindOneAsync(q =>
                    q.StudentQuizId == studentQuiz.Id &&
                    q.Status == Domain.Enums.QuizAttemptStatus.InProgress &&
                    (!quiz.TimeLimitMinutes.HasValue || q.StartedAt.AddMinutes(quiz.TimeLimitMinutes.Value) > now),
                    cancellationToken);

            if (ongoingAttempt != null)
            {
                _logger.LogInformation("Student already has an in-progress attempt with Id: {AttemptId}", ongoingAttempt.Id);
                return ongoingAttempt.ToGrpcQuizAttemptResponse();
            }

            var quizAttemptResponse = new GrpcQuizAttemptResponse();
            // Check attempt limitations with cooldown logic
            if (studentQuiz.MaxAttemptAllowed.HasValue)
            {
                if (studentQuiz.AttemptCount < studentQuiz.MaxAttemptAllowed.Value)
                {
                    // Still have attempts available
                    quizAttemptResponse = await CreateQuizAttempt(studentQuiz, quiz, cancellationToken);
                }
                else if (studentQuiz.NextAttemptAvailableAt.HasValue && now >= studentQuiz.NextAttemptAvailableAt.Value)
                {
                    // Cooldown finished, allow new attempt
                    quizAttemptResponse = await CreateQuizAttempt(studentQuiz, quiz, cancellationToken);
                }
                else
                {
                    // Cooldown not finished or no cooldown set
                    if (studentQuiz.NextAttemptAvailableAt.HasValue)
                    {
                        var timeRemaining = studentQuiz.NextAttemptAvailableAt.Value - now;
                        _logger.LogWarning("Cooldown period active for StudentQuizId: {StudentQuizId}. Time remaining: {TimeRemaining}",
                            request.StudentQuizId, timeRemaining);
                        throw new InvalidOperationException(
                            $"Bạn phải chờ thêm {timeRemaining.Hours} giờ và {timeRemaining.Minutes} phút nữa trước khi có thể làm lại bài quiz này.");
                    }
                    else
                    {
                        _logger.LogWarning("Maximum attempts reached for StudentQuizId: {StudentQuizId}", request.StudentQuizId);
                        throw new InvalidOperationException("Bạn đã đạt đến số lần làm tối đa cho bài quiz này.");
                    }
                }
            }
            else
            {
                // No attempt limit, create attempt
                quizAttemptResponse = await CreateQuizAttempt(studentQuiz, quiz, cancellationToken);
            }

            var quizAttempts = await _unitOfWork.QuizAttempts
                .FindAsync(q => q.StudentQuizId == request.StudentQuizId, cancellationToken);

            return quizAttemptResponse;
        }

        private async Task<GrpcQuizAttemptResponse> CreateQuizAttempt(Domain.Entities.StudentQuiz studentQuiz, QuizResponse quiz, CancellationToken cancellationToken)
        {
            var quizAttempt = new QuizAttempt
            {
                StudentQuizId = studentQuiz.Id,
                StartedAt = DateTime.UtcNow,
                Status = Domain.Enums.QuizAttemptStatus.InProgress,
                AttemptNumber = studentQuiz.AttemptCount + 1,
                TotalScore = 0
            };
            await _unitOfWork.QuizAttempts.AddAsync(quizAttempt, cancellationToken);

            // Update StudentQuiz attempt count and status  
            studentQuiz.AttemptCount += 1;

            // Set cooldown if reaching max attempts and cooldown is configured
            if (studentQuiz.MaxAttemptAllowed.HasValue &&
                studentQuiz.AttemptCount >= studentQuiz.MaxAttemptAllowed.Value &&
                quiz.CooldownHours.HasValue)
            {
                studentQuiz.NextAttemptAvailableAt = DateTime.UtcNow.AddHours(quiz.CooldownHours.Value);
                _logger.LogInformation("Cooldown set for StudentQuizId: {StudentQuizId} until {NextAttemptAvailableAt}",
                    studentQuiz.Id, studentQuiz.NextAttemptAvailableAt);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new quiz attempt for StudentQuizId: {StudentQuizId}, AttemptNumber: {AttemptNumber}",
                studentQuiz.Id, quizAttempt.AttemptNumber);

            return quizAttempt.ToGrpcQuizAttemptResponse();
        }
    }
}