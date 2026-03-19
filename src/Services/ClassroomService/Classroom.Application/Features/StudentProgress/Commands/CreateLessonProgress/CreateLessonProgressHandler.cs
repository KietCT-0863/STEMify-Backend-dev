
using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Features.StudentProgress.Commands.CreateSectionProgress;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentProgress.Commands.CreateLessonProgress
{
    public class CreateLessonProgressHandler : IRequestHandler<CreateLessonProgressCommand, Unit>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<CreateLessonProgressHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IGrpcLessonClient _grpcLessonClient;
        private readonly IGrpcSectionClient _grpcSectionClient;
        public CreateLessonProgressHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<CreateLessonProgressHandler> logger,
            IMediator mediator,
            IGrpcSectionClient grpcSectionClient,
            IGrpcLessonClient grpcLessonClient)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator;
            _grpcLessonClient = grpcLessonClient;
            _grpcSectionClient = grpcSectionClient;
        }
        public async Task<Unit> Handle(CreateLessonProgressCommand request, CancellationToken cancellationToken)
        {
            var lessonProgress = new StudentLessonProgress
            {
                EnrollmentId = request.CourseEnrollmentId,
                LessonId = request.LessonId,
                Status = request.Status
            };
            await _unitOfWork.LessonProgress.AddAsync(lessonProgress, cancellationToken);
            _logger.LogInformation("Created lesson progress for LessonId: {LessonId}, EnrollmentId: {EnrollmentId}, Status: {Status}",
                request.LessonId, request.CourseEnrollmentId, request.Status);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var querySectionsRequest = new QuerySectionsRequest
            {
                LessonId = request.LessonId,
                PageNumber = 1,
                PageSize = 1000,
                Status = "Published"
            };

            var sectionResponse = await _grpcSectionClient.GetSectionsAsync(querySectionsRequest);

            // Add section progress for each section in the lesson
            //await Parallel.ForEachAsync(sectionIds, cancellationToken, async (sectionId, ct) =>
            //{
            //    var createSectionProgressCommand = new CreateSectionProgressCommand
            //    {
            //        StudentLessonProgressId = lessonProgress.Id,
            //        SectionId = sectionId,
            //        Status = request.Status,
            //    };

            //    await _mediator.Send(createSectionProgressCommand, ct);
            //});

            var orderedSections = sectionResponse.Items.OrderBy(x => x.OrderIndex).ToList();
            bool isFirstSection = true;

            foreach (var section in orderedSections)
            {
                var status =
                    lessonProgress.Status == ProgressStatus.Locked
                    ? ProgressStatus.Locked
                    : (isFirstSection ? ProgressStatus.InProgress : ProgressStatus.Locked);

                await _mediator.Send(new CreateSectionProgressCommand
                {
                    StudentLessonProgressId = lessonProgress.Id,
                    SectionId = section.Id,
                    Status = status
                });

                isFirstSection = false;
            }
            return Unit.Value;
        }
    }
}
