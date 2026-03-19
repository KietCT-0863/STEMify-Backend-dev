using MediatR;
using Resource.Application.Commands.Agent;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Lessons;
using Shared.Exceptions;

namespace Resource.Application.Handlers.Agent
{
    public class SummarizeLessonCommandHandler : IRequestHandler<SummaryLessonCommand, IAsyncEnumerable<string>>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IAgentService _agentService;
        public SummarizeLessonCommandHandler(IResourceUnitOfWork unitOfWork, IAgentService agentService)
        {
            _unitOfWork = unitOfWork;
            _agentService = agentService;
        }
        public async Task<IAsyncEnumerable<string>> Handle(SummaryLessonCommand request, CancellationToken cancellationToken)
        {
            var spec = new LessonByIdSpecification(request.LessonId);
            var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(spec, cancellationToken);
            if (lesson == null)
            {
                throw new NotFoundException("Lesson not found");
            }
            var sectionContents = lesson.Sections
                                    .Where(s => s.Contents != null && s.Contents.Any())
                                    .Select(s => s.Contents.First().ContentBody)
                                    .ToList();
            var summaryHtmlSlides = _agentService.SummarizeSectionForPresentationAsync(
                                        $"Tóm tắt bài học {lesson.Title} để tạo slide trình chiếu dễ hiểu.",
                                        sectionContents
                                    );
            return summaryHtmlSlides;
        }
    }
}
