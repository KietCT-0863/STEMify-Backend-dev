using MediatR;

namespace Resource.Application.Commands.Agent
{
    public class AnswerGeneralStemQuestionCommand : IRequest<IAsyncEnumerable<string>>
    {
        public string UserPrompt { get; set; } = string.Empty;
    }

}
