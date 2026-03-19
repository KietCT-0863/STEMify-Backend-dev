using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.StudentProgress.Commands.CreateLessonProgress;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CourseEnrollments;
using Classroom.Domain.Enums;
using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.CourseEnrollments.Commands.CreateCourseEnrollment
{
    public class CreateCourseEnrollmentCommandHandler
        : IRequestHandler<CreateCourseEnrollmentCommand, CourseEnrollmentModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _userClient;
        private readonly ICourseCacheService _courseCache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMediator _mediator;
        private readonly IGrpcLessonClient _grpcLessonClient;
        private readonly ILogger<CreateCourseEnrollmentCommandHandler> _logger;

        public CreateCourseEnrollmentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcUserClient userClient,
            IPublishEndpoint publishEndpoint,
            ICourseCacheService courseCache,
            IMediator mediator,
            IGrpcLessonClient grpcLessonClient,
            ILogger<CreateCourseEnrollmentCommandHandler> logger
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _courseCache = courseCache ?? throw new ArgumentNullException(nameof(courseCache));
            _publishEndpoint =
                publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grpcLessonClient =
                grpcLessonClient ?? throw new ArgumentNullException(nameof(grpcLessonClient));
        }

        public async Task<CourseEnrollmentModel> Handle(
            CreateCourseEnrollmentCommand request,
            CancellationToken cancellationToken
        )
        {
            // Validate the student exists
            var student = await _userClient.GetOrganizationUserByIdAsync(request.StudentId, cancellationToken);
            // Validate the course exists
            var course = await _courseCache.GetByIdAsync(request.CourseId, cancellationToken);

            var courseEnrollmentSpec = new GetLatestActiveCourseEnrollmentSpecification
                            (request.StudentId, request.CourseId, request.CurriculumEnrollmentId, request.ClassroomId);
            var existingCourseEnrollment = await _unitOfWork.CourseEnrollments
                .FirstOrDefaultAsync(courseEnrollmentSpec, cancellationToken);

            if (existingCourseEnrollment != null)
                throw new InvalidOperationException(
                    $"The student is already enrolled in this course."
                );

            //var enrollmentModel = await _unitOfWork.ExecuteTransactionalAsync(async () =>
            //{
            //    var courseEnrollment = request.ToEnrollmentEntity();
            //    courseEnrollment.Status = request.Status;

            //    await _unitOfWork.CourseEnrollments.AddAsync(courseEnrollment, cancellationToken);
            //    await _unitOfWork.SaveChangesAsync(cancellationToken);

            //    foreach (var lessonId in course.LessonIds)
            //    {
            //        await _mediator.Send(new CreateLessonProgressCommand
            //        {
            //            CourseEnrollmentId = courseEnrollment.Id,
            //            LessonId = lessonId,
            //            Status = ProgressStatus.InProgress
            //        }, cancellationToken);
            //    }

            //    return courseEnrollment.ToEnrollmentModel(course);
            //}, cancellationToken);

            var courseEnrollment = request.ToEnrollmentEntity();
            await _unitOfWork.CourseEnrollments.AddAsync(courseEnrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created course enrollment with ID {CourseEnrollmentId}", courseEnrollment.Id);

            //await Parallel.ForEachAsync(course.LessonIds, cancellationToken, async (lessonId, ct) =>
            //{
            //    await _mediator.Send(new CreateLessonProgressCommand
            //    {
            //        CourseEnrollmentId = courseEnrollment.Id,
            //        LessonId = lessonId,
            //        Status = ProgressStatus.InProgress
            //    }, ct);
            //});

            //foreach (var lesson in course.Lessons)
            //{
            //    await _mediator.Send(new CreateLessonProgressCommand
            //    {
            //        CourseEnrollmentId = courseEnrollment.Id,
            //        LessonId = lesson.Id,
            //        Status = ProgressStatus.InProgress
            //    });
            //}
            // Sort lessons by orderIndex

            var queryLessonRequest = new QueryLessonsRequest
            {
                CourseId = course.Id,
                PageNumber = 1,
                PageSize = 100,
                Status = "Published"
            };
            var response = await _grpcLessonClient.GetLessonsAsync(queryLessonRequest);

            bool isFirstLesson = true;

            foreach (var lesson in response.Items.OrderBy(x => x.OrderIndex))
            {
                var status = isFirstLesson ? ProgressStatus.InProgress : ProgressStatus.Locked;

                await _mediator.Send(new CreateLessonProgressCommand
                {
                    CourseEnrollmentId = courseEnrollment.Id,
                    LessonId = lesson.Id,
                    Status = status
                });

                isFirstLesson = false;
            }


            var enrollmentModel = courseEnrollment.ToEnrollmentModel(course);

            // Publish events to queue
            var enrollmentCreated = new CourseEnrollmentCreatedEvent
            {
                Id = courseEnrollment.Id,
                StudentId = courseEnrollment.StudentId.ToString(),
                StudentName = student?.FullName ?? string.Empty,
                StudentEmail = student?.Email ?? string.Empty,
                CourseId = courseEnrollment.CourseId,
                CourseTitle = course.Title,
                EnrolledAt = courseEnrollment.EnrolledAt,
            };
            await _publishEndpoint.Publish(enrollmentCreated);

            // Save all changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return enrollmentModel;
        }
    }
}
