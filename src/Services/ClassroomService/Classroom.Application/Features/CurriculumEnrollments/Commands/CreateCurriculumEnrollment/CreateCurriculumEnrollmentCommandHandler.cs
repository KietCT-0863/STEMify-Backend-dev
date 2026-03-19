using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.CourseEnrollments.Commands.CreateCourseEnrollment;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CurriculumEnrollments;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using EventBus.Messages;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Classroom.Application.Features.CurriculumEnrollments.Commands.CreateCurriculumEnrollment
{
    public class CreateCurriculumEnrollmentCommandHandler
        : IRequestHandler<CreateCurriculumEnrollmentCommand, CurriculumEnrollmentModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _userClient;
        private readonly ICurriculumCacheService _curriculumCache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMediator _mediator;
        private readonly ILogger<CreateCurriculumEnrollmentCommandHandler> _logger;

        public CreateCurriculumEnrollmentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcUserClient userClient,
            IPublishEndpoint publishEndpoint,
            ICourseCacheService courseCache,
            ICurriculumCacheService curriculumCache,
            IMediator mediator,
            ILogger<CreateCurriculumEnrollmentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _curriculumCache = curriculumCache ?? throw new ArgumentNullException(nameof(curriculumCache));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _mediator = mediator;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CurriculumEnrollmentModel> Handle(
            CreateCurriculumEnrollmentCommand request,
            CancellationToken cancellationToken)
        {

            var student = await _userClient.GetOrganizationUserByIdAsync(request.StudentId, cancellationToken)
                ?? throw new InvalidOperationException("Student not found.");

            var curriculum = await _curriculumCache.GetByIdAsync(request.CurriculumId, cancellationToken)
                ?? throw new InvalidOperationException("Curriculum not found.");

            var enrollment = await CreateCurriculumEnrollment(request, cancellationToken);

            //await AutoEnrollCourseAsync(request, student, curriculum, enrollment, cancellationToken);
            var orderedCourses = curriculum.Courses.OrderBy(c => c.OrderIndex).ToList();
            var firstCourse = orderedCourses.FirstOrDefault();
            if (firstCourse != null)
                await EnrollFirstCourseAsInProgressAsync(request.StudentId, firstCourse, enrollment.Id, request.ClassroomId, cancellationToken);

            await PublishCurriculumEnrollmentCreatedEventAsync(enrollment, student, curriculum, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return enrollment.ToEnrollmentModel(curriculum);

        }

        private async Task<CurriculumEnrollment> CreateCurriculumEnrollment(
            CreateCurriculumEnrollmentCommand request,
            CancellationToken cancellationToken)
        {
            var enrollment = request.ToEnrollmentEntity();
            var spec = new GetCurriculumEnrollmentSpecification
                (enrollment.StudentId, enrollment.CurriculumId);
            var existing = await _unitOfWork.CurriculumEnrollments
                .FirstOrDefaultAsync(spec, cancellationToken);

            if (existing == null)
            {
                enrollment.Status = request.Status;
                await _unitOfWork.CurriculumEnrollments.AddAsync(enrollment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return enrollment;
            }

            throw new InvalidOperationException("The student is already enrolled in this curriculum.");
        }

        //private async Task AutoEnrollCourseAsync(
        //    CreateCurriculumEnrollmentCommand request,
        //    UserModel student,
        //    CurriculumModel curriculum,
        //    CurriculumEnrollment enrollment,
        //    CancellationToken cancellationToken)
        //{
        //    var orderedCourses = curriculum.Courses.OrderBy(c => c.OrderIndex).ToList();

        //    if (enrollment.Status == EnrollmentStatus.InProgress)
        //    {
        //        var firstCourse = orderedCourses.FirstOrDefault();
        //        if (firstCourse != null)
        //        {
        //            await EnrollFirstCourseAsInProgressAsync(request.StudentId, student, firstCourse, cancellationToken);
        //        }
        //    }
        //}

        //private async Task EnrollAllCoursesAsNotStartedAsync(
        //    Guid studentId,
        //    List<CourseDetail> orderedCourses,
        //    CancellationToken cancellationToken)
        //{
        //    foreach (var courseDetail in orderedCourses)
        //    {
        //        var spec = new GetLatestActiveCourseEnrollmentSpecification
        //                    (studentId, courseDetail.Id);
        //        var existing = await _unitOfWork.CourseEnrollments
        //            .FirstOrDefaultAsync(spec, cancellationToken);

        //        if (existing == null)
        //        {
        //            var courseEnrollment = new CourseEnrollment
        //            {
        //                StudentId = studentId,
        //                CourseId = courseDetail.Id,
        //                Status = EnrollmentStatus.NotStarted
        //            };

        //            await _unitOfWork.CourseEnrollments.AddAsync(courseEnrollment, cancellationToken);
        //            await _unitOfWork.SaveChangesAsync(cancellationToken);
        //            await CreateLessonProgressAsync(courseEnrollment, courseDetail.Id, cancellationToken);
        //        }
        //    }

        //    await _unitOfWork.SaveChangesAsync(cancellationToken);
        //}

        private async Task EnrollFirstCourseAsInProgressAsync(
            Guid studentId,
            CourseDetail firstCourse,
            int curriculumEnrollmentId,
            int? classroomId,
            CancellationToken cancellationToken)
        {
            var createCourseEnrollmentCommand = new CreateCourseEnrollmentCommand
            {
                StudentId = studentId,
                CourseId = firstCourse.Id,
                Status = EnrollmentStatus.InProgress,
                CurriculumEnrollmentId = curriculumEnrollmentId,
                ClassroomId = classroomId
            };
            await _mediator.Send(createCourseEnrollmentCommand, cancellationToken);
        }

        private async Task PublishCurriculumEnrollmentCreatedEventAsync(
            CurriculumEnrollment enrollment,
            OrganizationUserInfo student,
            CurriculumModel curriculum,
            CancellationToken cancellationToken)
        {
            var eventMessage = new CurriculumEnrollmentCreatedEvent
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId.ToString(),
                StudentName = student?.FullName ?? string.Empty,
                StudentEmail = student?.Email ?? string.Empty,
                CurriculumId = enrollment.CurriculumId,
                CurriculumTitle = curriculum.Title,
                EnrolledAt = enrollment.EnrolledAt,
            };
            await _publishEndpoint.Publish(eventMessage, cancellationToken);
        }
    }
}