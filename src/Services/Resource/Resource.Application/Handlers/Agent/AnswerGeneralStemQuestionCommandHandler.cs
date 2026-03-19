using MediatR;
using Resource.Application.Commands.Agent;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Agent
{
    public class AnswerGeneralStemQuestionCommandHandler : IRequestHandler<AnswerGeneralStemQuestionCommand, IAsyncEnumerable<string>>
    {
        private readonly IAgentService _agentService;

        public AnswerGeneralStemQuestionCommandHandler(IAgentService agentService)
        {
            _agentService = agentService;
        }
        public Task<IAsyncEnumerable<string>> Handle(AnswerGeneralStemQuestionCommand request, CancellationToken cancellationToken)
        {
            // Call the AgentService
            var result = _agentService.AnswerGeneralStemQuestionAsync(request.UserPrompt);
            return Task.FromResult(result);
        }
    }
}
