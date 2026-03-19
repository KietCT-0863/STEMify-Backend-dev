using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Specifications.StudentAssignment;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentById
{
    public class GetStudentAssignmentByIdQueryHandler : IRequestHandler<GetStudentAssignmentByIdQuery, GrpcStudentAssignmentResponse>
    {
        private readonly ILogger<GetStudentAssignmentByIdQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcRubricCriterionClient _grpcRubricCriterionClient;

        public GetStudentAssignmentByIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcRubricCriterionClient grpcRubricCriterionClient,
            ILogger<GetStudentAssignmentByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _grpcRubricCriterionClient = grpcRubricCriterionClient;
            _logger = logger;
        }

        public async Task<GrpcStudentAssignmentResponse> Handle(GetStudentAssignmentByIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetStudentAssignmentByIdSpecification(request.Id);
            var studentAssignment = await _unitOfWork.StudentAssignments
                .FirstOrDefaultAsync(spec, cancellationToken);
            if (studentAssignment == null)
            {
                _logger.LogWarning("StudentAssignment with Id: {Id} not found", request.Id);
                throw new KeyNotFoundException($"StudentAssignment with Id {request.Id} not found.");
            }

            var response = studentAssignment.ToGprcStudentAssignmentResponse();

            var existingRubricIds = response.Attempts
                .SelectMany(a => a.QuestionAttempts)
                .SelectMany(q => q.RubricScore)
                .Select(r => r.RubricCriterionId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (existingRubricIds.Any())
            {
                var fetchTasks = existingRubricIds.ToDictionary(
                    id => id,
                    id => _grpcRubricCriterionClient.GetRubricCriterionByIdAsync(id)
                );

                await Task.WhenAll(fetchTasks.Values);

                var rubricById = fetchTasks.ToDictionary(kv => kv.Key, kv => kv.Value.Result);

                foreach (var attempt in response.Attempts)
                {
                    foreach (var questionAttempt in attempt.QuestionAttempts)
                    {
                        for (int i = 0; i < questionAttempt.RubricScore.Count; i++)
                        {
                            var score = questionAttempt.RubricScore[i];
                            if (score == null) continue;
                            if (rubricById.TryGetValue(score.RubricCriterionId, out var criterion) && criterion != null)
                            {
                                score.CriterionName = criterion.CriterionName ?? string.Empty;
                                score.Description = criterion.Description;
                                score.MaxPoints = criterion.MaxPoints;
                                questionAttempt.RubricScore[i] = score;
                            }
                        }
                    }
                }
            }

            var questionAttemptsWithoutScores = response.Attempts
                .SelectMany(a => a.QuestionAttempts)
                .Where(q => q.RubricScore == null || q.RubricScore.Count == 0)
                .ToList();

            if (questionAttemptsWithoutScores.Any())
            {
                var questionIds = questionAttemptsWithoutScores
                    .Select(q => q.AssignmentQuestionId)
                    .Distinct()
                    .ToList();

                var fetchByQuestionTasks = questionIds.ToDictionary(
                    id => id,
                    id => _grpcRubricCriterionClient.GetQueryRubricCriterions(
                        new QueryRubricCriterionsRequest
                        {
                            AssignmentQuestionId = id,
                            PageNumber = 1,
                            PageSize = 100
                        })
                );

                await Task.WhenAll(fetchByQuestionTasks.Values);

                var criterionsByQuestion = fetchByQuestionTasks.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.Result?.Items?.ToList() ?? new List<RubricCriterionResponse>());

                foreach (var questionAttempt in questionAttemptsWithoutScores)
                {
                    if (criterionsByQuestion.TryGetValue(questionAttempt.AssignmentQuestionId, out var criterions) && criterions.Any())
                    {
                        foreach (var crit in criterions)
                        {
                            var model = new GrpcRubricScoreModel
                            {
                                RubricCriterionId = crit.Id,
                                CriterionName = crit.CriterionName,
                                Description = crit.Description,
                                MaxPoints = crit.MaxPoints
                            };
                            questionAttempt.RubricScore.Add(model);
                        }
                    }
                }
            }

            return response;
        }
    }
}