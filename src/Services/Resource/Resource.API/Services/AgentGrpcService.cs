using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Agent;
using Shared.Protos.Agent;

namespace Resource.API.Services
{
    public class AgentGrpcService : GrpcAgentService.GrpcAgentServiceBase
    {
        private readonly IMediator _mediator;
        public AgentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async override Task AnswerGeneralStemQuestion(SendMessageRequest request, IServerStreamWriter<GenerateMessageResponse> responseStream, ServerCallContext context)
        {
            var command = new AnswerGeneralStemQuestionCommand
            {
                UserPrompt = request.UserPrompt
            };

            await foreach (var chunk in await _mediator.Send(command))
            {
                await responseStream.WriteAsync(new GenerateMessageResponse
                {
                    Message = chunk
                });
            }
        }
        public async override Task GenerateCourseRecommendation(SendMessageRequest request, IServerStreamWriter<GenerateMessageResponse> responseStream, ServerCallContext context)
        {
            var command = new GenerateCourseRecommendationCommand
            {
                UserPrompt = request.UserPrompt
            };

            await foreach (var chunk in await _mediator.Send(command))
            {
                await responseStream.WriteAsync(new GenerateMessageResponse
                {
                    Message = chunk
                });
            }
        }

        public async override Task SummarizeLesson(SummarizeLessonRequest request, IServerStreamWriter<GenerateMessageResponse> responseStream, ServerCallContext context)
        {
            var command = new SummaryLessonCommand
            {
                LessonId = request.LessonId
            };

            await foreach (var chunk in await _mediator.Send(command))
            {
                await responseStream.WriteAsync(new GenerateMessageResponse
                {
                    Message = chunk
                });
            }
        }
    }
}
