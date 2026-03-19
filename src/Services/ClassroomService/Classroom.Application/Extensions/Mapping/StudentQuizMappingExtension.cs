
using Classroom.Domain.Entities;
using Shared.Protos.Classroom;

namespace Classroom.Application.Extensions.Mapping
{
    public static class StudentQuizMappingExtension
    {
        public static GrpcStudentQuizResponse ToGprcStudentQuizResponse(this StudentQuiz studentQuiz)
        {
            if (studentQuiz == null)
                throw new ArgumentNullException(nameof(studentQuiz));
            var response = new GrpcStudentQuizResponse
            {
                Id = studentQuiz.Id,
                StudentId = studentQuiz.StudentId,
                QuizId = studentQuiz.QuizId,
                AssignedAt = studentQuiz.AssignedAt.ToString("o"),
                DueDate = studentQuiz.DueDate?.ToString("o"),
                Status = studentQuiz.Status.ToString(),
                AttemptCount = studentQuiz.AttemptCount,
                MaxAttemptAllowed = studentQuiz.MaxAttemptAllowed,
                FinalScore = (long?)studentQuiz.FinalScore,
                NextAttemptAvailableAt = studentQuiz.NextAttemptAvailableAt.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(studentQuiz.NextAttemptAvailableAt.Value.ToUniversalTime())
                    : null
            };
            response.Attempts.AddRange(studentQuiz.QuizAttempts
                .Select(qa => qa.ToGrpcQuizAttemptResponse()));
            return response;
        }
        public static GrpcQuizAttemptResponse ToGrpcQuizAttemptResponse(this QuizAttempt quizAttempt)
        {
            if (quizAttempt == null)
                throw new ArgumentNullException(nameof(quizAttempt));
            var response = new GrpcQuizAttemptResponse
            {
                Id = quizAttempt.Id,
                StudentQuizId = quizAttempt.StudentQuizId,
                StartedAt = quizAttempt.StartedAt.ToString("o"),
                CompletedAt = quizAttempt.CompletedAt?.ToString("o"),
                Status = quizAttempt.Status.ToString(),
                AttemptNumber = quizAttempt.AttemptNumber,
                TotalScore = (long?)quizAttempt.TotalScore
            };
            response.QuestionAttempts.AddRange(quizAttempt.QuestionAttempts
                .Select(qa => qa.ToGrpcQuestionAttemptResponse()));
            return response;
        }

        public static GrpcQuestionAttemptResponse ToGrpcQuestionAttemptResponse(this QuizQuestionAttempt questionAttempt)
        {
            if (questionAttempt == null) throw new ArgumentNullException(nameof(questionAttempt));
            var response = new GrpcQuestionAttemptResponse
            {
                QuestionId = questionAttempt.QuestionId,
                IsCorrect = questionAttempt.IsCorrect,
                Score = (double)(questionAttempt.Score ?? 0),
            };
            response.AnswerAttempts.AddRange(questionAttempt.AnswerAttempts
                .Select(aa => aa.ToGrpcAnswerAttemptResponse()));
            return response;
        }

        public static GrpcAnswerAttemptResponse ToGrpcAnswerAttemptResponse(this AnswerAttempt answerAttempt)
        {
            if (answerAttempt == null) throw new ArgumentNullException(nameof(answerAttempt));
            var response = new GrpcAnswerAttemptResponse
            {
                AnswerId = answerAttempt.AnswerId,
                IsSelected = answerAttempt.IsSelected,
                IsCorrect = answerAttempt.IsCorrect
            };
            return response;
        }
    }
}
