using MediatR;
using Newtonsoft.Json;
using Resource.Application.Commands.Agent;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Agent
{
    public class GenerateCourseRecommendationCommandHandler : IRequestHandler<GenerateCourseRecommendationCommand, IAsyncEnumerable<string>>
    {
        private readonly IAgentService _agentService;
        private readonly IResourceUnitOfWork _unitOfWork;

        public GenerateCourseRecommendationCommandHandler(IAgentService agentService, IResourceUnitOfWork unitOfWork)
        {
            _agentService = agentService;
            _unitOfWork = unitOfWork;
        }
        public async Task<IAsyncEnumerable<string>> Handle(GenerateCourseRecommendationCommand request, CancellationToken cancellationToken)
        {
            // Fetch all courses from the database
            var courses = await _unitOfWork.Courses.GetAllAsync();
            // Serialize the course list to JSON
            var courseJson = JsonConvert.SerializeObject(courses);
            // Call the AgentService
            var result = _agentService.GenerateCourseRecommendationsAsync(request.UserPrompt, courseJson);
            return result;
        }
    }
}
