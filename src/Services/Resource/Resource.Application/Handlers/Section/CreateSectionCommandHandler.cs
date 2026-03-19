using MediatR;
using Resource.Application.Commands.Section;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Lessons;
using Resource.Application.Specifications.Sections;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Section
{
    public class CreateSectionCommandHandler
        : IRequestHandler<CreateSectionCommand, SectionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateSectionCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SectionResponse> Handle(
            CreateSectionCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new LessonByIdSpecification(request.LessonId);
            var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(spec, cancellationToken);
            if (lesson == null)
            {
                throw new KeyNotFoundException(
                    $"Lesson with ID {request.LessonId} does not exist."
                );
            }

            var existingSections = await _unitOfWork.Sections.GetAllAsync(
                new PublishedSectionByLessonIdSpecification(request.LessonId),
                cancellationToken
            );

            var nextOrderIndex = existingSections.Any()
                ? existingSections.Max(s => s.OrderIndex) + 1
                : 0;

            var section = new Domain.Entities.Section
            {
                Title = request.Title,
                Description = request.Description,
                Duration = request.Duration,
                Status = Domain.Enums.SectionStatus.Published,
                LessonId = request.LessonId,
                OrderIndex = nextOrderIndex,
                IsVisibleToStudent = request.IsVisibleToStudent,
            };

            await _unitOfWork.Sections.AddAsync(section, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var sectionsAfterInsert = await _unitOfWork.Sections.GetAllAsync(
                new PublishedSectionByLessonIdSpecification(request.LessonId),
                cancellationToken
            );

            lesson.Duration = sectionsAfterInsert.Sum(s => s.Duration);

            await _unitOfWork.Lessons.UpdateAsync(lesson);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new SectionResponse
            {
                Id = section.Id,
                Title = section.Title,
                Description = section.Description,
                Duration = section.Duration,
                Status = section.Status.ToString(),
                OrderIndex = section.OrderIndex,
                LessonId = section.LessonId,
                IsVisibleToStudent = section.IsVisibleToStudent,
            };

            return response;
        }
    }
}
