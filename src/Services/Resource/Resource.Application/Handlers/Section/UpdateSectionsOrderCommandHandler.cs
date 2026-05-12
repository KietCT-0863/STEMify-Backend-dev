using MediatR;
using Resource.Application.Commands.Section;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Lessons;
using Shared.Exceptions;

namespace Resource.Application.Handlers.Section
{
    public class UpdateSectionsOrderCommandHandler
        : IRequestHandler<UpdateSectionsOrderCommand, Unit>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateSectionsOrderCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            UpdateSectionsOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new LessonByIdSpecification(request.LessonId);
            var lesson =
                await _unitOfWork.Lessons.FirstOrDefaultAsync(spec, cancellationToken)
                ?? throw new NotFoundException($"Lesson with {request.LessonId} not found");

            var sectionIds = lesson.Sections.Select(s => s.Id).ToHashSet();
            if (!sectionIds.SetEquals(request.OrderedSectionIds))
                throw new DomainException("ordered ids must match sections of the lesson");

            for (int i = 0; i < request.OrderedSectionIds.Count; i++)
            {
                var id = request.OrderedSectionIds[i];
                var s = await _unitOfWork.Sections.FindByIdForUpdateAsync(id, cancellationToken);
                if (s != null && s.OrderIndex != i)
                {
                    s.OrderIndex = i;
                    // ?? PERFORMANCE: Explicitly call UpdateAsync to attach entity
                    // Required because global NoTracking is enabled
                    await _unitOfWork.Sections.UpdateAsync(s, cancellationToken);
                }

                lesson.LastModifiedDate = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
