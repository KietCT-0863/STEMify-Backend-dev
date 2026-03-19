using Classroom.Application.Features.StudentAssignment.Commands.CreateStudentAssignmentAttempt;
using Classroom.Application.Features.StudentAssignment.Commands.UpdateStudentAssignmentAttempt;
using Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptById;
using Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptList;
using Grpc.Core;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class AssignmentAttemptGrpcService : GrpcAssignmentAttempt.GrpcAssignmentAttemptBase
    {
        private readonly IMediator _mediator;

        public AssignmentAttemptGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcAssignmentAttemptResponse> CreateAssignmentAttempt(CreateAssignmentAttemptRequest request, ServerCallContext context)
        {
            var command = new CreateStudentAssignmentAttemptCommand
            {
                StudentAssignmentId = request.StudentAssignmentId,
                AssignmentQuestionAttempts = request.QuestionAttempts.Select(qa => new AssignmentQuestionAttemptCommand
                {
                    AssignmentQuestionId = qa.AssignmentQuestionId,
                    AnswerText = qa.AnswerText,
                    AnswerFile = qa.AnswerFile?.ToByteArray()
                }).ToList()
            };
            return await _mediator.Send(command);
        }

        public override async Task<GrpcAssignmentAttemptResponse> UpdateAssignmentAttempt(UpdateAssignmentAttemptRequest request, ServerCallContext context)
        {
            var grades = new List<QuestionGradeCommand>();

            foreach (var qg in request.QuestionGrades)
            {
                var rubricScores = new List<RubricScoreCommand>();

                if (qg.RubricScores != null)
                {
                    foreach (var rs in qg.RubricScores)
                    {
                        rubricScores.Add(new RubricScoreCommand
                        {
                            RubricCriterionId = rs.RubricCriterionId,
                            Points = (decimal)rs.Points
                        });
                    }
                }

                grades.Add(new QuestionGradeCommand
                {
                    AssignmentQuestionAttemptId = qg.AssignmentQuestionAttemptId,
                    RubricScores = rubricScores
                });
            }

            var command = new UpdateStudentAssignmentAttemptCommand
            {
                Id = request.Id,
                Feedback = request.Feedback,
                Grades = grades
            };

            return await _mediator.Send(command);
        }

        public override async Task<GrpcAssignmentAttemptResponse> GetAssignmentAttemptById(GetAssignmentAttemptByIdRequest request, ServerCallContext context)
        {
            var query = new GetAssignmentAttemptByIdQuery
            {
                Id = request.Id
            };
            return await _mediator.Send(query);
        }

        public override Task<GrpcPagedAssignmentAttemptsResponse> GetPagedAssignmentAttempts(GetAssignmentAttemptParams request, ServerCallContext context)
        {
            var query = new GetPagedAssignmentAttemptsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                OrderBy = request.OrderBy,
                StudentId = request.StudentId,
                FromDate = request.FromDate?.ToDateTime(),
                ToDate = request.ToDate?.ToDateTime(),
            };

            return _mediator.Send(query);
        }
    }
}
