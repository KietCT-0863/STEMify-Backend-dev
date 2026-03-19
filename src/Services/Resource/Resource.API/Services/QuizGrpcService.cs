using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Quiz;
using Resource.Application.Queries.Assignment;
using Resource.Application.Queries.Quiz;
using Shared.Extensions;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class QuizGrpcService : QuizService.QuizServiceBase
    {
        private readonly IMediator _mediator;

        public QuizGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<QuizResponse> CreateQuiz(
            CreateQuizRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateQuizCommand
            {
                Title = request.Title,
                TotalMarks = request.TotalMarks,
                PassingMarks = request.PassingMarks,
                SectionId = request.SectionId,
                DurationDays = request.DurationDays,
                Description = request.Description,
                TimeLimitMinutes = request.TimeLimitMinutes,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<QuizResponse> GetQuiz(
            GetQuizRequest request,
            ServerCallContext context
        )
        {
            var query = new GetQuizByIdQuery(request.Id);
            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<QuizResponse> UpdateQuiz(
            UpdateQuizRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateQuizCommand
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                TotalMarks = request.TotalMarks,
                PassingMarks = request.PassingMarks,
                DurationDays = request.DurationDays,
                TimeLimitMinutes = request.TimeLimitMinutes,
                CooldownHours = request.CooldownHours,
                MaxAttemptAllowed = request.MaxAttemptAllowed,
                Status = request.Status.ToEnumOrNull<Domain.Enums.ContentStatus>(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteQuiz(
            DeleteQuizRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteQuizCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<QuizList> ListQuizzes(Empty request, ServerCallContext context)
        {
            var result = await _mediator.Send(new GetQuizListQuery());
            return result;
        }

        public override async Task<QuizImportResult> ImportQuizQuestions(ImportQuizQuestionsRequest request, ServerCallContext context)
        {
            var command = new ImportQuizQuestionsCommand()
            {
                QuizId = request.Id,
                CsvFileBytes = request.CsvFile.ToByteArray(),
            };
            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<QuizQuestionsTemplate> GetQuizQuestionsTemplate(Empty request, ServerCallContext context)
        {
            var result = await _mediator.Send(new GetQuizTemplateQuery());
            return result;
        }

        public override async Task<PagedQuizList> QueryQuizzes(
            QueryQuizzesRequest request,
            ServerCallContext context
        )
        {
            var query = new QueryQuizzesQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
            };
            var result = await _mediator.Send(query);
            return result;
        }
    }
}
