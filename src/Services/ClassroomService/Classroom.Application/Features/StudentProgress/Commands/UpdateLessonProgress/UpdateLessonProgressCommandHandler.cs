using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.CourseEnrollments.Commands.UpdateCourseEnrollment;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.ProgressModels;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Commands.UpdateLessonProgress
{
    public class UpdateLessonProgressCommandHandler
        : IRequestHandler<UpdateLessonProgressCommand, StudentLessonProgressModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcLessonClient _grpcLessonClient;
        private readonly IGrpcCourseClient _grpcCourseClient;
        private readonly IGrpcSectionClient _grpcSectionClient;
        private readonly IMediator _mediator;

        public UpdateLessonProgressCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcLessonClient grpcLessonClient,
            IGrpcCourseClient grpcCourseClient,
            IGrpcSectionClient grpcSectionClient,
            IMediator mediator
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _grpcLessonClient =
                grpcLessonClient ?? throw new ArgumentNullException(nameof(grpcLessonClient));
            _grpcCourseClient =
                grpcCourseClient ?? throw new ArgumentNullException(nameof(grpcCourseClient));
            _grpcSectionClient =
                grpcSectionClient ?? throw new ArgumentNullException(nameof(grpcSectionClient));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<StudentLessonProgressModel> Handle(
            UpdateLessonProgressCommand request,
            CancellationToken cancellationToken
        )
        {
            var lessonProgress = await _unitOfWork.LessonProgress.FindByIdAsync(
                request.LessonProgressId,
                cancellationToken
            );
            if (lessonProgress == null)
            {
                throw new InvalidOperationException(
                    "Lesson progress not found."
                );
            }
            lessonProgress.Status = request.Status;
            if (request.Status == ProgressStatus.Completed)
            {
                lessonProgress.CompletedAt = DateTime.UtcNow;
            }

            await _unitOfWork.LessonProgress.UpdateAsync(lessonProgress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (lessonProgress.Status == ProgressStatus.Completed)
            {
                await UnlockNextLesson(lessonProgress, cancellationToken);

                await UpdateCourseEnrollment(lessonProgress.EnrollmentId, cancellationToken);
            }

            return new StudentLessonProgressModel
            {
                Id = lessonProgress.Id,
                LessonId = lessonProgress.LessonId,
                Status = lessonProgress.Status.ToString(),
            };
        }

        // Update the overall course enrollment progress based on lesson progress

        private async Task UpdateCourseEnrollment(int enrollmentId, CancellationToken cancellationToken)
        {
            var allLessonProgress = await _unitOfWork.LessonProgress.FindAsync(
                                            lp => lp.EnrollmentId == enrollmentId,
                                            cancellationToken
                                        );
            if (allLessonProgress != null && allLessonProgress.Count > 0)
            {
                var progressPercentage = allLessonProgress.Count(lp => lp.Status == ProgressStatus.Completed) * 100 /
                                     allLessonProgress.Count;
                var updateEnrollmentCommand = new UpdateCourseEnrollmentCommand
                {
                    Id = enrollmentId,
                    ProgressPercentage = progressPercentage,
                    Status = progressPercentage == 100 ? EnrollmentStatus.Completed : null
                };
                await _mediator.Send(updateEnrollmentCommand, cancellationToken);
            }
        }

        private async Task UnlockNextLesson(StudentLessonProgress current, CancellationToken cancellationToken)
        {
            // 1. Load all lesson progress for the enrollment
            var allProgress = await _unitOfWork.LessonProgress.FindAsync(
                lp => lp.EnrollmentId == current.EnrollmentId,
                cancellationToken
            );

            var progressList = allProgress.ToList();
            if (progressList.Count == 0) return;

            // 2. Collect unique SectionIds
            var lessonIds = progressList
                .Select(x => x.LessonId)
                .Distinct()
                .ToList();

            // 3. Build ordered lesson metadata map
            var lessonMap = new Dictionary<int, LessonModel>();

            foreach (var id in lessonIds)
            {
                var lesson = await _grpcLessonClient.GetLessonByIdAsync(id);
                if (lesson != null)
                    lessonMap[id] = lesson;
            }

            // 4. Merge progress + metadata, sort by OrderIndex
            var ordered = progressList
                .Select(p => new
                {
                    Progress = p,
                    Lesson = lessonMap.ContainsKey(p.LessonId) ? lessonMap[p.LessonId] : null
                })
                .Where(x => x.Lesson != null)
                .OrderBy(x => x.Lesson.OrderIndex)
                .ToList();

            // 5. Locate current lesson
            var index = ordered.FindIndex(x => x.Progress.Id == current.Id);
            if (index < 0 || index == ordered.Count - 1)
                return; // current is last lesson

            var next = ordered[index + 1].Progress;

            // 6. Unlock if locked
            if (next.Status == ProgressStatus.Locked)
            {
                next.Status = ProgressStatus.InProgress;
                await _unitOfWork.LessonProgress.UpdateAsync(next, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); 

                await UnlockFirstSectionOfLesson(next, cancellationToken);
            }
        }

        private async Task UnlockFirstSectionOfLesson(
            StudentLessonProgress lessonProgress,
            CancellationToken cancellationToken)
        {
            // Load all section progress of this lesson
            var sections = await _unitOfWork.SectionProgress.FindAsync(
                sp => sp.StudentLessonProgressId == lessonProgress.Id,
                cancellationToken
            );

            var list = sections.ToList();
            if (!list.Any())
                return;

            // Fetch metadata for all sectionIds
            var sectionIds = list.Select(x => x.SectionId).Distinct().ToList();
            var metadata = new Dictionary<int, Shared.Protos.Resource.SectionResponse>();

            foreach (var id in sectionIds)
            {
                var s = await _grpcSectionClient.GetSectionByIdAsync(id);
                if (s != null)
                    metadata[id] = s;
            }

            // Determine FIRST section by OrderIndex
            var first = list
                .Select(p => new
                {
                    Progress = p,
                    Section = metadata.ContainsKey(p.SectionId) ? metadata[p.SectionId] : null
                })
                .Where(x => x.Section != null)
                .OrderBy(x => x.Section.OrderIndex)
                .FirstOrDefault();

            if (first == null)
                return;

            // Unlock first section if locked
            if (first.Progress.Status == ProgressStatus.Locked)
            {
                first.Progress.Status = ProgressStatus.InProgress;

                await _unitOfWork.SectionProgress.UpdateAsync(first.Progress, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

    }
}
