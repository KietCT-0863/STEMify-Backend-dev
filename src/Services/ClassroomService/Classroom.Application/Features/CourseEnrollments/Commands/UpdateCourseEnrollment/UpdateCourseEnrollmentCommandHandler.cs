using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.CurriculumEnrollments.Commands.UpdateCurriculumEnrollment;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Classroom.Application.Features.CourseEnrollments.Commands.UpdateCourseEnrollment
{
    public class UpdateCourseEnrollmentCommandHandler : IRequestHandler<UpdateCourseEnrollmentCommand, CourseEnrollmentModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IGrpcCourseClient _grpcCourseClient;
        private readonly IGrpcUserClient _userClient;
        private readonly IMediator _mediator;
        private readonly ICurriculumCacheService _curriculumCache;
        private readonly ILogger<UpdateCourseEnrollmentCommandHandler> _logger;
        public UpdateCourseEnrollmentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            IGrpcCourseClient grpcCourseClient,
            IGrpcUserClient userClient,
            IMediator mediator,
            ICurriculumCacheService curriculumCache,
            ILogger<UpdateCourseEnrollmentCommandHandler> logger
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _grpcCourseClient = grpcCourseClient ?? throw new ArgumentNullException(nameof(grpcCourseClient));
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _curriculumCache = curriculumCache ?? throw new ArgumentNullException(nameof(curriculumCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<CourseEnrollmentModel> Handle(UpdateCourseEnrollmentCommand request, CancellationToken cancellationToken)
        {
            var courseEnrollment = await _unitOfWork.CourseEnrollments
                                    .FindByIdAsync(request.Id, cancellationToken);
            if (courseEnrollment == null)
            {
                throw new KeyNotFoundException($"Course enrollment with ID {request.Id} not found.");
            }
            if (request.Status.HasValue)
                courseEnrollment.Status = request.Status.Value;
            if (request.Status.HasValue && request.Status.Value == EnrollmentStatus.Completed)
                courseEnrollment.CompletedAt = DateTime.UtcNow;
            if (request.ProgressPercentage.HasValue)
                courseEnrollment.ProgressPercentage = request.ProgressPercentage.Value;

            await _unitOfWork.CourseEnrollments.UpdateAsync(courseEnrollment, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

           await _publishEndpoint.Publish(
                new ClassroomStudentProgressUpdatedEvent
                {
                    StudentId = courseEnrollment.StudentId.ToString(),
                    ClassroomId = courseEnrollment.ClassroomId,
                    CourseEnrollmentId = courseEnrollment.Id,
                    CourseId = courseEnrollment.CourseId,
                    ProgressPercentage = courseEnrollment.ProgressPercentage,
                    Status = courseEnrollment.Status.ToString()
                },
                cancellationToken
            );

            // Publish event if enrollment is completed and create course certificate
            if (courseEnrollment.Status == EnrollmentStatus.Completed)
            {

                var student = await _userClient.GetOrganizationUserByIdAsync(courseEnrollment.StudentId, cancellationToken);
                var course = await _grpcCourseClient.GetCourseByIdAsync(courseEnrollment.CourseId);

                // Create course completion certificate
                //var createCertificateCommand = new CreateCertificateCommand
                //{
                //    User = student,
                //    CertificateType = CertificateType.Course,
                //    CourseEnrollmentId = courseEnrollment.Id
                //};
                //await _mediator.Send(createCertificateCommand, cancellationToken);
                await _publishEndpoint.Publish(new CertificateGenerationRequestedEvent
                {
                    UserId = student.UserId,
                    CertificateType = CertificateType.Course.ToString(),
                    CourseEnrollmentId = courseEnrollment.Id
                }, cancellationToken);


                // Publish CourseCompletedEvent
                await _publishEndpoint.Publish(
                new CourseCompletedEvent
                {
                    Id = courseEnrollment.Id,
                    StudentId = courseEnrollment.StudentId.ToString(),
                    StudentName = student?.FullName ?? "Unknown Student",
                    StudentEmail = student?.Email ?? string.Empty,
                    CourseId = courseEnrollment.CourseId,
                    CourseTitle = course.Title,
                    CompletedAt = courseEnrollment.CompletedAt ?? DateTime.UtcNow,
                },
                    cancellationToken
                );

                await CheckAndCreateCurriculumCertificateAsync(courseEnrollment, cancellationToken);
            }

            var enrollmentModel = courseEnrollment.ToEnrollmentModel();

            return enrollmentModel;
        }

        private async Task CheckAndCreateCurriculumCertificateAsync(
            CourseEnrollment enrollment,
            CancellationToken cancellationToken)
        {
            // Find all curriculum enrollments for this user that are in progress
            var curriculumEnrollments = await _unitOfWork.CurriculumEnrollments.FindAsync(
                ce => ce.StudentId == enrollment.StudentId && ce.Status == EnrollmentStatus.InProgress,
                cancellationToken
            );

            _logger.LogInformation("Found {Count} curriculum enrollments for student {StudentId}", curriculumEnrollments.Count, enrollment.StudentId);

            foreach (var curriculumEnrollment in curriculumEnrollments)
            {
                // Get curriculum details (including all courses) from cache
                var curriculum = await _curriculumCache.GetByIdAsync(curriculumEnrollment.CurriculumId, cancellationToken);
                if (curriculum == null || curriculum.Courses == null || curriculum.Courses.Count == 0)
                    continue;

                int totalCourses = curriculum.Courses.Count;
                int completedCourses = 0;

                foreach (var course in curriculum.Courses)
                {
                    var courseEnrollment = await _unitOfWork.CourseEnrollments.FindOneAsync(
                        ce => ce.StudentId == curriculumEnrollment.StudentId
                            && ce.CourseId == course.Id
                            && ce.Status == EnrollmentStatus.Completed,
                        cancellationToken
                    );
                    if (courseEnrollment != null)
                    {
                        completedCourses++;
                    }
                }

                var progresPercentage = totalCourses > 0
                    ? (int)Math.Round((completedCourses * 100.0) / totalCourses)
                    : 0;
                var updateCurriculumEnrollmentCommand = new UpdateCurriculumEnrollmentCommand
                {
                    Id = curriculumEnrollment.Id,
                    ProgressPercentage = progresPercentage,
                    Status = progresPercentage == 100 ? EnrollmentStatus.Completed : null,
                    CurriculumId = curriculumEnrollment.CurriculumId
                };
                await _mediator.Send(updateCurriculumEnrollmentCommand, cancellationToken);
            }
        }
    }
}
