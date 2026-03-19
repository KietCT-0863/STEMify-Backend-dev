using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Assignment;
using Resource.Application.Queries.Assignment;
using ServiceStack;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class AssignmentGrpcService : GrpcAssignmentService.GrpcAssignmentServiceBase
    {
        private readonly IMediator _mediator;

        public AssignmentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcAssignmentModel> CreateAssignment(
            CreateAssignmentRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateAssignmentCommand
            {
                SectionId = request.SectionId,
                Title = request.Title,
                PassingScore = (decimal)request.PassingScore,
                DurationDays = request.DurationDays,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                AssignmentQuestions = request.Questions
                    .Select(q => new CreateAssignmentQuestionModel
                    {
                        AssignmentQuestionType = q.Type.ToEnumOrDefault(Domain.Enums.AssignmentQuestionType.Text),
                        Content = q.Content,
                        OrderIndex = q.OrderIndex,
                        RubricCriterion = q.RubricCriterion
                            .Select(r => new CreateRubricCriterionModel
                            {
                                CriterionName = r.CriterionName,
                                Description = r.Description,
                                MaxPoints = (decimal)r.MaxPoints
                            })
                            .ToList()
                    })
                    .ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcAssignmentModel> GetAssignmentById(
            GetAssignmentRequest request,
            ServerCallContext context
        )
        {
            var query = new GetAssignmentByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Assignment with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<GrpcAssignmentModel> UpdateAssignment(
            UpdateAssignmentRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateAssignmentsCommand
            {
                Id = request.Id,
                Title = request.Title,
                PassingScore = (decimal?)request.PassingScore,
                DurationDays = request.DurationDays,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                AssignmentQuestions = request.Questions
                    .Select(q => new UpdateAssignmentQuestionModel
                    {
                        Id = q.Id,
                        AssignmentQuestionType = q.Type.ToEnumOrDefault(Domain.Enums.AssignmentQuestionType.Text),
                        Content = q.Content,
                        OrderIndex = q.OrderIndex,
                        Points = (decimal)q.Points
                    })
                    .ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteAssignment(
            DeleteAssignmentRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteAssignmentsCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<AssignmentImportResult> ImportAssignmentQuestions(ImportAssignmentQuestionsRequest request, ServerCallContext context)
        {
            var command = new ImportAssignmentQuestionsCommand()
            {
                AssignmentId = request.Id,
                CsvFileBytes = request.CsvFile.ToByteArray(),
            };
            var result = await _mediator.Send(command);

            return result;
        }

        public override async Task<AssignmentQuestionsTemplate> GetAssignmentQuestionsTemplate(Empty request, ServerCallContext context)
        {
            var result = await _mediator.Send(new GetAssignmentTemplateQuery());
            return result;
        }
    }
}