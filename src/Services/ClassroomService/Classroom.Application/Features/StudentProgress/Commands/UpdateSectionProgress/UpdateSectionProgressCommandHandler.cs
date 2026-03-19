using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.StudentProgress.Commands.UpdateLessonProgress;
using Classroom.Application.Models.ProgressModels;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentProgress.Commands.UpdateSectionProgress
{
    public class UpdateSectionProgressCommandHandler
        : IRequestHandler<UpdateSectionProgressCommand, StudentSectionProgressModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcSectionClient _grpcSectionClient;
        private readonly IMediator _mediator;

        public UpdateSectionProgressCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcSectionClient grpcSectionClient,
            IMediator mediator
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _grpcSectionClient =
                grpcSectionClient ?? throw new ArgumentNullException(nameof(grpcSectionClient));
        }

        public async Task<StudentSectionProgressModel> Handle(
            UpdateSectionProgressCommand request,
            CancellationToken cancellationToken
        )
        {
            StudentSectionProgress? sectionProgress = null;
            if (request.SectionProgressId.HasValue)
            {
                sectionProgress = await _unitOfWork.SectionProgress.FindByIdAsync(
                    request.SectionProgressId.Value,
                    cancellationToken
                );
            }
            else
            {
                sectionProgress = await _unitOfWork.SectionProgress.FindOneAsync(
                sp =>
                    sp.LessonProgress.EnrollmentId == request.EnrollmentId
                    && sp.LessonProgress.LessonId == request.LessonId
                    && sp.SectionId == request.SectionId,
                cancellationToken
            );
            }

            if (sectionProgress == null)
                throw new InvalidOperationException("Section progress not found.");

            if (request.Status == ProgressStatus.Completed && sectionProgress.Status != ProgressStatus.Completed)
                sectionProgress.CompletedAt = DateTime.UtcNow;
            sectionProgress.Status = request.Status;

            await _unitOfWork.SectionProgress.UpdateAsync(sectionProgress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Nếu section hoàn thành, cập nhật lesson progress
            if (request.Status == ProgressStatus.Completed)
            {
                await UnlockNextSection(sectionProgress, cancellationToken);

                await UpdateLessonProgress(
                    sectionProgress.StudentLessonProgressId,
                    cancellationToken
                );
            }

            return new StudentSectionProgressModel
            {
                Id = sectionProgress.Id,
                SectionId = sectionProgress.SectionId,
                Status = sectionProgress.Status.ToString(),
                CompletedAt = sectionProgress.CompletedAt
            };
        }


        private async Task UpdateLessonProgress(
            int lessonProgressId,
            CancellationToken cancellationToken
        )
        {
            var notCompletedSection = await _unitOfWork.SectionProgress.AnyAsync(
                sp =>
                    sp.StudentLessonProgressId == lessonProgressId
                    && sp.Status != ProgressStatus.Completed,
                cancellationToken
            );

            if (notCompletedSection)
                return;

            // All sections are completed, update lesson progress to completed
            await _mediator.Send(
                new UpdateLessonProgressCommand
                {
                    LessonProgressId = lessonProgressId,
                    Status = ProgressStatus.Completed
                },
                cancellationToken
            );
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task UnlockNextSection(
            StudentSectionProgress current,
            CancellationToken cancellationToken)
        {
            // 1. Load all progress rows for this lesson
            var allProgress = await _unitOfWork.SectionProgress.FindAsync(
                sp => sp.StudentLessonProgressId == current.StudentLessonProgressId,
                cancellationToken
            );

            var progressList = allProgress.ToList();
            if (!progressList.Any()) return;

            // 2. Collect unique SectionIds
            var sectionIds = progressList
                .Select(x => x.SectionId)
                .Distinct()
                .ToList();

            // 3. Fetch each Section from Resource service (1 request per ID)
            var sectionMap = new Dictionary<int, SectionResponse>();

            foreach (var id in sectionIds)
            {
                var section = await _grpcSectionClient.GetSectionByIdAsync(id);
                if (section != null)
                {
                    sectionMap[id] = section;
                }
            }

            // 4. Merge progress & section metadata, sort by OrderIndex
            var ordered = progressList
                .Select(p => new
                {
                    Progress = p,
                    Section = sectionMap.ContainsKey(p.SectionId) ? sectionMap[p.SectionId] : null
                })
                .Where(x => x.Section != null)
                .OrderBy(x => x.Section.OrderIndex)
                .ToList();

            // 5. Find the current index
            var index = ordered.FindIndex(x => x.Progress.Id == current.Id);
            if (index < 0 || index == ordered.Count - 1)
                return; // current is last section

            // 6. Identify next section progress
            var next = ordered[index + 1].Progress;

            // 7. Unlock next section if locked
            if (next.Status == ProgressStatus.Locked)
            {
                next.Status = ProgressStatus.InProgress;

                await _unitOfWork.SectionProgress.UpdateAsync(next, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }
}