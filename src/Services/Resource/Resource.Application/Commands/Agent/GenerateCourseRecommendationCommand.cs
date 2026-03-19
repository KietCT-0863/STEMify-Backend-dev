using MediatR;

namespace Resource.Application.Commands.Agent
{
    public class GenerateCourseRecommendationCommand : IRequest<IAsyncEnumerable<string>>
    {
        public string UserPrompt { get; set; } = string.Empty;
    }
}
