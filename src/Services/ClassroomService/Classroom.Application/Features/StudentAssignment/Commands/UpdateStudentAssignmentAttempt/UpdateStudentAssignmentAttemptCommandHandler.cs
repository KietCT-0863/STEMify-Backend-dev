using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptById;
using Classroom.Application.Features.StudentProgress.Commands.UpdateSectionProgress;
using Classroom.Application.Specifications.AssignmentAttempt;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Commands.UpdateStudentAssignmentAttempt
{
    public class UpdateStudentAssignmentAttemptCommandHandler : IRequestHandler<UpdateStudentAssignmentAttemptCommand, GrpcAssignmentAttemptResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateStudentAssignmentAttemptCommandHandler> _logger;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcRubricCriterionClient _grpcRubricCriterionClient;
        private readonly IMediator _mediator;

        public UpdateStudentAssignmentAttemptCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<UpdateStudentAssignmentAttemptCommandHandler> logger,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcRubricCriterionClient grpcRubricCriterionClient,
            IMediator mediator)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcAssignmentClient = grpcAssignmentClient;
            _grpcRubricCriterionClient = grpcRubricCriterionClient;
            _mediator = mediator;
        }

        public async Task<GrpcAssignmentAttemptResponse> Handle(UpdateStudentAssignmentAttemptCommand request, CancellationToken cancellationToken)
        {
            var spec = new GetAssignmentAttemptByIdSpecification(request.Id);
            var assignmentAttempt = await _unitOfWork.AssignmentAttempts.FirstOrDefaultAsync(spec, cancellationToken);

            if (assignmentAttempt == null)
            {
                _logger.LogWarning("AssignmentAttempt with Id: {Id} not found", request.Id);
                throw new KeyNotFoundException($"AssignmentAttempt with Id {request.Id} not found.");
            }

            // Validate attempt is not already graded
            if (assignmentAttempt.Status != AssignmentAttemptStatus.UnderReview)
            {
                _logger.LogWarning("AssignmentAttempt {Id} has already been graded", request.Id);
                throw new InvalidOperationException("This assignment attempt has already been graded.");
            }

            var studentAssignment = await _unitOfWork.StudentAssignments.FindByIdAsync(assignmentAttempt.StudentAssignmentId, cancellationToken);
            if (studentAssignment == null)
            {
                _logger.LogWarning("StudentAssignment with Id: {Id} not found", assignmentAttempt.StudentAssignmentId);
                throw new KeyNotFoundException($"StudentAssignment with Id {assignmentAttempt.StudentAssignmentId} not found.");
            }

            // Get assignment details
            var assignment = await _grpcAssignmentClient.GetAssignmentByIdAsync(studentAssignment.AssignmentId);

            // Process each question grade
            foreach (var gradeRequest in request.Grades)
            {
                var questionAttempt = assignmentAttempt.AssignmentQuestionAttempts
                    .FirstOrDefault(qa => qa.Id == gradeRequest.AssignmentQuestionAttemptId);

                if (questionAttempt == null)
                {
                    _logger.LogWarning("AssignmentQuestionAttempt {Id} not found in AssignmentAttempt {AttemptId}",
                        gradeRequest.AssignmentQuestionAttemptId, request.Id);
                    throw new KeyNotFoundException($"Question attempt {gradeRequest.AssignmentQuestionAttemptId} not found.");
                }

                // Validate and create rubric scores
                decimal questionTotalPoints = 0m;
                var rubricScores = new List<RubricScore>();

                foreach (var rubricScoreRequest in gradeRequest.RubricScores)
                {
                    // Validate rubric criterion and max points
                    var rubricCriterion = await _grpcRubricCriterionClient.GetRubricCriterionByIdAsync(rubricScoreRequest.RubricCriterionId);

                    if (rubricCriterion == null)
                    {
                        _logger.LogWarning("RubricCriterion {Id} not found", rubricScoreRequest.RubricCriterionId);
                        throw new KeyNotFoundException($"RubricCriterion {rubricScoreRequest.RubricCriterionId} not found.");
                    }

                    if (rubricScoreRequest.Points > (decimal)rubricCriterion.MaxPoints)
                    {
                        _logger.LogWarning("Points {Points} exceed max points {MaxPoints} for RubricCriterion {Id}",
                            rubricScoreRequest.Points, rubricCriterion.MaxPoints, rubricScoreRequest.RubricCriterionId);
                        throw new InvalidOperationException(
                            $"Points {rubricScoreRequest.Points} exceed max points {rubricCriterion.MaxPoints} for criterion {rubricScoreRequest.RubricCriterionId}");
                    }

                    rubricScores.Add(new RubricScore
                    {
                        AssignmentQuestionAttemptId = questionAttempt.Id,
                        RubricCriterionId = rubricScoreRequest.RubricCriterionId,
                        Points = rubricScoreRequest.Points
                    });

                    questionTotalPoints += rubricScoreRequest.Points;
                }

                // Clear existing rubric scores and add new ones
                if (questionAttempt.RubricScores != null && questionAttempt.RubricScores.Any())
                {
                    await _unitOfWork.RubricScores.DeleteRangeAsync(questionAttempt.RubricScores.ToList(), cancellationToken);
                }

                await _unitOfWork.RubricScores.AddRangeAsync(rubricScores, cancellationToken);
                questionAttempt.Points = questionTotalPoints;
            }

            // Calculate total score
            decimal totalPointsEarned = assignmentAttempt.AssignmentQuestionAttempts.Sum(qa => qa.Points);
            decimal totalPossiblePoints = assignment.Questions.Sum(q => (decimal)q.Points);
            decimal totalScorePercentage = totalPossiblePoints > 0
                ? Math.Round(totalPointsEarned / totalPossiblePoints * 100, 2)
                : 0;

            assignmentAttempt.TotalScore = totalScorePercentage;

            // Update statuses based on passing score
            bool passed = totalScorePercentage >= (decimal)assignment.PassingScore;

            assignmentAttempt.Status = passed ? AssignmentAttemptStatus.Passed : AssignmentAttemptStatus.Failed;
            studentAssignment.Status = passed ? StudentAssignmentStatus.Passed : StudentAssignmentStatus.Failed;

            // Update final score (keep best score across all attempts)
            if (!studentAssignment.FinalScore.HasValue || totalScorePercentage > studentAssignment.FinalScore.Value)
            {
                studentAssignment.FinalScore = totalScorePercentage;
            }

            // Save feedback if provided
            if (request.Feedback != null)
            {
                assignmentAttempt.Feedback = request.Feedback;
            }

            // If passed, update section progress
            if (passed)
            {
                var command = new UpdateSectionProgressCommand
                {
                    SectionProgressId = studentAssignment.StudentSectionProgressId,
                    Status = ProgressStatus.Completed
                };
                await _mediator.Send(command, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Graded AssignmentAttempt {Id} | Score={Score}% | Status={Status}",
                assignmentAttempt.Id, totalScorePercentage, assignmentAttempt.Status);

            var query = new GetAssignmentAttemptByIdQuery { Id = assignmentAttempt.Id };
            return await _mediator.Send(query, cancellationToken);
        }
    }
}