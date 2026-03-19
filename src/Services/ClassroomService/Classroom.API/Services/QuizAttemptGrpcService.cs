using Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuizAttempt;
using Classroom.Application.Features.StudentQuiz.Commands.UpdateStudentQuizAttempt;
using Classroom.Application.Features.StudentQuiz.Queries.GetQuizAttemptList;
using Grpc.Core;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class QuizAttemptGrpcService : GrpcQuizAttempt.GrpcQuizAttemptBase
    {
        private readonly IMediator _mediator;
        public QuizAttemptGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }
        public override async Task<GrpcQuizAttemptResponse> CreateQuizAttempt(CreateQuizAttemptRequest request, ServerCallContext context)
        {
            var command = new CreateStudentQuizAttemptCommand
            {
                StudentQuizId = request.StudentQuizId,
            };
            return await _mediator.Send(command);
        }

        public override async Task<GrpcQuizAttemptResponse> UpdateQuizAttempt(UpdateQuizAttemptRequest request, ServerCallContext context)
        {
            var command = new UpdateStudentQuizAttemptCommand
            {
                Id = request.Id,
                QuestionAttempts = request.QuestionAttempts.Select(qa => new QuestionAttemptCommand
                {
                    QuestionId = qa.QuestionId,
                    AnswerIds = qa.AnswerIds.ToList()
                }).ToList()
            };
            return await _mediator.Send(command);
        }

        public override Task<GrpcQuizAttemptResponse> GetQuizAttemptById(GetQuizAttemptByIdRequest request, ServerCallContext context)
        {
            return base.GetQuizAttemptById(request, context);
        }

        public override Task<GrpcPagedQuizAttemptsResponse> GetPagedQuizAttempts(GetQuizAttemptParams request, ServerCallContext context)
        {
            var query = new GetPagedQuizAttemptsQuery
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
