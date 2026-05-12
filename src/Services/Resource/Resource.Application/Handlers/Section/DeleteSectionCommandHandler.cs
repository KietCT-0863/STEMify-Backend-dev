using MediatR;
using Resource.Application.Commands.Section;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Sections;

namespace Resource.Application.Handlers.Section
{
    public class DeleteSectionCommandHandler : IRequestHandler<DeleteSectionCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public DeleteSectionCommandHandler(IResourceUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
        {
            var section = await _unitOfWork.Sections.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (section == null)
                throw new KeyNotFoundException($"Section with ID {request.Id} not found.");

            // If the section is in Draft status, delete it permanently.
            if (section.Status == Domain.Enums.SectionStatus.Draft)
                await _unitOfWork.Sections.DeleteAsync(section, cancellationToken);
            else
            {
                // For Published sections, mark them as Deleted.
                section.Status = Domain.Enums.SectionStatus.Deleted;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var sections = (await _unitOfWork.Sections.GetAllAsync(
                new PublishedSectionByLessonIdSpecification(section.LessonId),
                cancellationToken
            )).OrderBy(s => s.OrderIndex).ToList();

            for (var i = 0; i < sections.Count; i++)
            {
                var s = sections[i];
                if (s.OrderIndex != i)
                {
                    s.OrderIndex = i;
                    await _unitOfWork.Sections.UpdateAsync(s);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var duration = sections.Sum(s => s.Duration);

            var updateLessonCommand = new Commands.Lesson.UpdateLessonCommand
            {
                Id = section.LessonId,
                Duration = duration
            };

            await _mediator.Send(updateLessonCommand, cancellationToken);
        }
    }
}
