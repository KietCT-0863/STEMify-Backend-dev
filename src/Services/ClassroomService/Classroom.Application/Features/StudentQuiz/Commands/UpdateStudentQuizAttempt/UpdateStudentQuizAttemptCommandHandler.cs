using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.StudentProgress.Commands.UpdateSectionProgress;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentQuiz.Commands.UpdateStudentQuizAttempt
{
    public class UpdateStudentQuizAttemptCommandHandler : IRequestHandler<UpdateStudentQuizAttemptCommand, GrpcQuizAttemptResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateStudentQuizAttemptCommandHandler> _logger;
        private readonly IGrpcQuizClient _grpcQuizClient;
        private readonly IMediator _mediator;
        public UpdateStudentQuizAttemptCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<UpdateStudentQuizAttemptCommandHandler> logger,
            IGrpcQuizClient grpcQuizClient,
            IMediator mediator)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcQuizClient = grpcQuizClient;
            _mediator = mediator;
        }
        public async Task<GrpcQuizAttemptResponse> Handle(UpdateStudentQuizAttemptCommand request, CancellationToken cancellationToken)
        {
            var quizAttempt = await _unitOfWork.QuizAttempts.FindByIdAsync(request.Id, cancellationToken);
            if (quizAttempt == null)
            {
                _logger.LogWarning("QuizAttempt with Id: {Id} not found", request.Id);
                throw new KeyNotFoundException($"QuizAttempt with Id {request.Id} not found.");
            }

            var studentQuiz = await _unitOfWork.StudentQuizzes.FindByIdAsync(quizAttempt.StudentQuizId, cancellationToken);
            if (studentQuiz == null)
            {
                _logger.LogWarning("StudentQuiz with Id: {Id} not found", quizAttempt.StudentQuizId);
                throw new KeyNotFoundException($"StudentQuiz with Id {quizAttempt.StudentQuizId} not found.");
            }
            var quiz = await _grpcQuizClient.GetQuizByIdAsync(studentQuiz.QuizId);

            if (quizAttempt.QuestionAttempts != null && quizAttempt.QuestionAttempts.Count() > 0)
            {
                _logger.LogWarning("QuizAttempt with Id {AttemptId} has already been submitted and cannot be modified.", quizAttempt.Id);
                throw new InvalidOperationException($"QuizAttempt {quizAttempt.Id} has already been submitted.");
            }
            var (questionAttempts, totalScore) = CreateQuestionAttemptsAndCalculateScore(quiz, request.QuestionAttempts);
            quizAttempt.QuestionAttempts = questionAttempts;

            UpdateStatuses(quizAttempt, studentQuiz, quiz, totalScore);

            if (studentQuiz.Status == StudentQuizStatus.Passed)
            {
                var command = new UpdateSectionProgressCommand
                {
                    SectionProgressId = studentQuiz.StudentSectionProgressId,
                    Status = ProgressStatus.Completed
                };
                await _mediator.Send(command, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("QuizAttempt {Id} submitted | Score={Score} | Status={Status}",
                 quizAttempt.Id, totalScore, quizAttempt.Status);

            return quizAttempt.ToGrpcQuizAttemptResponse();
        }

        private static (List<QuizQuestionAttempt>, decimal) CreateQuestionAttemptsAndCalculateScore(
            QuizResponse quiz,
            List<QuestionAttemptCommand> requestAttempts)
        {
            var questionAttempts = new List<QuizQuestionAttempt>();
            decimal totalPoints = 0m;
            decimal quizTotalPoints = quiz.Questions.Sum(q => q.Points);

            foreach (var req in requestAttempts)
            {
                var quizQuestion = quiz.Questions.FirstOrDefault(q => q.Id == req.QuestionId);
                if (quizQuestion == null)
                    continue;

                // Tạo danh sách AnswerAttempt cho từng answer được chọn
                var answerAttempts = new List<AnswerAttempt>();
                foreach (var answer in quizQuestion.Answers)
                {
                    bool isSelected = req.AnswerIds.Contains(answer.Id);
                    bool isCorrect = answer.IsCorrect;

                    answerAttempts.Add(new AnswerAttempt
                    {
                        AnswerId = answer.Id,
                        IsSelected = isSelected,
                        IsCorrect = isCorrect
                    });
                }

                // Kiểm tra đúng sai ở cấp độ câu hỏi
                bool isQuestionCorrect = CheckAnswerCorrectness(quizQuestion, req.AnswerIds);

                // Tính điểm
                decimal score = isQuestionCorrect ? quizQuestion.Points : 0;
                totalPoints += score;

                // Gộp thành QuestionAttempt
                var questionAttempt = new QuizQuestionAttempt
                {
                    QuestionId = quizQuestion.Id,
                    IsCorrect = isQuestionCorrect,
                    Score = score,
                    AnswerAttempts = answerAttempts
                };

                questionAttempts.Add(questionAttempt);
            }

            // Tổng điểm (%)
            decimal totalScore = quizTotalPoints == 0 ? 0 : Math.Round(totalPoints / quizTotalPoints * 100, 2);

            return (questionAttempts, totalScore);
        }

        private static bool CheckAnswerCorrectness(QuestionResponse quizQuestion, List<int> selectedAnswerIds)
        {
            var correctIds = quizQuestion.Answers
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .OrderBy(id => id)
                .ToList();

            var submittedIds = selectedAnswerIds.OrderBy(id => id).ToList();

            // Chỉ đúng nếu chọn đúng và đủ tất cả đáp án đúng, không chọn thừa
            return correctIds.SequenceEqual(submittedIds);
        }


        private static void UpdateStatuses(
            QuizAttempt quizAttempt,
            Domain.Entities.StudentQuiz studentQuiz,
            QuizResponse quiz,
            decimal totalScore)
        {
            bool passed = totalScore >= (decimal)quiz.PassingMarks;

            quizAttempt.TotalScore = totalScore;
            quizAttempt.Status = passed ? QuizAttemptStatus.Passed : QuizAttemptStatus.Failed;
            quizAttempt.CompletedAt = DateTime.UtcNow;

            if (studentQuiz.Status != StudentQuizStatus.Passed)
            {
                studentQuiz.FinalScore = totalScore;
                studentQuiz.Status = passed ? StudentQuizStatus.Passed : StudentQuizStatus.Failed;
            }
        }
    }
}
