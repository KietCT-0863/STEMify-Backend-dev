
using Classroom.Domain.Entities;
using Shared.Protos.Classroom;

namespace Classroom.Application.Extensions.Mapping
{
    public static class StudentAssignmentMappingExtension
    {
        public static GrpcStudentAssignmentResponse ToGprcStudentAssignmentResponse(this StudentAssignment studentAssignment)
        {
            if (studentAssignment == null)
                throw new ArgumentNullException(nameof(studentAssignment));
            var response = new GrpcStudentAssignmentResponse
            {
                Id = studentAssignment.Id,
                StudentId = studentAssignment.StudentId,
                AssignmentId = studentAssignment.AssignmentId,
                AssignedAt = studentAssignment.AssignedAt.ToString("o"),
                DueDate = studentAssignment.DueDate?.ToString("o"),
                Status = studentAssignment.Status.ToString(),
                AttemptCount = studentAssignment.AttemptCount,
                MaxAttemptAllowed = studentAssignment.MaxAttemptAllowed,
                FinalScore = (long?)studentAssignment.FinalScore,
                NextAttemptAvailableAt = studentAssignment.NextAttemptAvailableAt.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(studentAssignment.NextAttemptAvailableAt.Value.ToUniversalTime())
                    : null
            };
            response.Attempts.AddRange(studentAssignment.AssignmentAttempts
                .Select(qa => qa.ToGrpcAssignmentAttemptResponse()));
            return response;
        }
        public static GrpcAssignmentAttemptResponse ToGrpcAssignmentAttemptResponse(this AssignmentAttempt quizAttempt)
        {
            if (quizAttempt == null)
                throw new ArgumentNullException(nameof(quizAttempt));
            var response = new GrpcAssignmentAttemptResponse
            {
                Id = quizAttempt.Id,
                StudentAssignmentId = quizAttempt.StudentAssignmentId,
                Feedback = quizAttempt.Feedback ?? string.Empty,
                TeacherId = quizAttempt.TeacherId,
                SubmittedAt = quizAttempt.SubmittedAt.ToString("o"),
                Status = quizAttempt.Status.ToString(),
                AttemptNumber = quizAttempt.AttemptNumber,
                TotalScore = (double)quizAttempt.TotalScore
            };
            response.QuestionAttempts.AddRange(quizAttempt.AssignmentQuestionAttempts
                .Select(qa => qa.ToGrpcQuestionAttemptResponse()));
            return response;
        }

        public static GrpcAssignmentQuestionAttemptResponse ToGrpcQuestionAttemptResponse(this AssignmentQuestionAttempt questionAttempt)
        {
            if (questionAttempt == null) throw new ArgumentNullException(nameof(questionAttempt));
            var response = new GrpcAssignmentQuestionAttemptResponse
            {
                Id = questionAttempt.Id,
                AssignmentAttemptId = questionAttempt.AssignmentAttemptId,
                AssignmentQuestionId = questionAttempt.AssignmentQuestionId,
                AnswerText = questionAttempt.AnswerText ?? string.Empty,
                AnswerFileUrl = questionAttempt.AnswerFileUrl ?? string.Empty,
                Points = (double)questionAttempt.Points
            };
            response.RubricScore.AddRange(questionAttempt.RubricScores
                .Select(aa => aa.ToGrpcAnswerAttemptResponse()));
            return response;
        }

        public static GrpcRubricScoreModel ToGrpcAnswerAttemptResponse(this RubricScore answerAttempt)
        {
            if (answerAttempt == null) throw new ArgumentNullException(nameof(answerAttempt));
            var response = new GrpcRubricScoreModel
            {
                CurrentPoints = (double?)answerAttempt.Points,
                RubricCriterionId = answerAttempt.RubricCriterionId
            };
            return response;
        }
    }
}
