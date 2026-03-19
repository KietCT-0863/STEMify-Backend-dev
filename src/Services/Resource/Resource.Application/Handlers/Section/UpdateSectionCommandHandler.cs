using MediatR;
using Resource.Application.Commands.Section;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Sections;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Section
{
    public class UpdateSectionCommandHandler
        : IRequestHandler<UpdateSectionCommand, SectionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public UpdateSectionCommandHandler(IResourceUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<SectionResponse> Handle(
            UpdateSectionCommand request,
            CancellationToken cancellationToken
        )
        {
            var section = await _unitOfWork.Sections.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (section == null)
                throw new KeyNotFoundException($"Section with ID {request.Id} not found.");

            if (request.Description != null)
                section.Description = request.Description;
            if (request.Title != null)
                section.Title = request.Title;
            if (request.Duration.HasValue)
                section.Duration = request.Duration.Value;
            if (request.IsVisibleToStudent.HasValue)
                section.IsVisibleToStudent = request.IsVisibleToStudent.Value;
            if (request.Status.HasValue)
            {
                section.Status = request.Status.Value;
                foreach (var content in section.Contents)
                {
                    content.Status = (Domain.Enums.ContentStatus)request.Status.Value;
                }
            }

            await _unitOfWork.Sections.UpdateAsync(section, cancellationToken);
            // Update the lesson's total duration after updating the section
            var sections = await _unitOfWork.Sections.GetAllAsync(
                new PublishedSectionByLessonIdSpecification(section.LessonId),
                cancellationToken
            );
            int duration = 0;
            foreach (var l in sections)
            {
                duration += l.Duration;
            }

            var updateLessonCommand = new Commands.Lesson.UpdateLessonCommand
            {
                Id = section.LessonId,
                Duration = duration
            };

            await _mediator.Send(updateLessonCommand, cancellationToken);

            var response = new SectionResponse
            {
                Id = section.Id,
                Title = section.Title,
                Description = section.Description,
                Duration = section.Duration,
                Status = section.Status.ToString(),
                OrderIndex = section.OrderIndex,
                LessonId = section.LessonId,
                IsVisibleToStudent = section.IsVisibleToStudent
            };
            response.ContentIds.AddRange(
                section.Contents?.Select(x => x.Id) ?? Enumerable.Empty<int>()
            );

            return response;
        }
    }
}
