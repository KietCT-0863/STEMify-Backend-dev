using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using EventBus.Messages;
using MassTransit;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.CurriculumEnrollments.Commands.UpdateCurriculumEnrollment
{
    public class UpdateCurriculumEnrollmentCommandHandler
        : IRequestHandler<UpdateCurriculumEnrollmentCommand, CurriculumEnrollmentModel>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCache;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMediator _mediator;
        private readonly IGrpcUserClient _userClient;

        public UpdateCurriculumEnrollmentCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ICourseCacheService courseCache,
            ICurriculumCacheService curriculumCache,
            IMediator mediator,
            IUserCacheService userCache,
            IPublishEndpoint publishEndpoint,
            IGrpcUserClient userClient
            )
        {
            _unitOfWork = unitOfWork;
            _curriculumCache = curriculumCache;
            _userClient = userClient;
            _publishEndpoint = publishEndpoint;
            _mediator = mediator;
        }

        public async Task<CurriculumEnrollmentModel> Handle(UpdateCurriculumEnrollmentCommand request, CancellationToken cancellationToken)
        {
            // Lấy curriculum enrollment
            var enrollment = await _unitOfWork.CurriculumEnrollments.FindByIdAsync(request.Id, cancellationToken);
            if (enrollment == null)
            {
                throw new NotFoundException($"Enrollment with ID {request.Id} not found.");
            }

            var curriculum = await _curriculumCache.GetByIdAsync(request.CurriculumId, cancellationToken)
                ?? throw new InvalidOperationException("Curriculum not found.");

            // Update status curriculum enrollment
            if (request.Status.HasValue)
                enrollment.Status = (EnrollmentStatus)request.Status;
            if (request.ProgressPercentage.HasValue)
                enrollment.ProgressPercentage = request.ProgressPercentage.Value;
            if (request.Status.HasValue && request.Status.Value == EnrollmentStatus.Completed)
                enrollment.CompletedAt = DateTime.UtcNow;

            await _unitOfWork.CurriculumEnrollments.UpdateAsync(enrollment, cancellationToken);

            // Nếu là InProgress thì update course enrollment đầu tiên
            //if (request.Status == EnrollmentStatus.InProgress)
            //{
            //    var firstCourse = curriculum.Courses.OrderBy(c => c.OrderIndex).FirstOrDefault();

            //    if (firstCourse != null)
            //    {
            //        var courseEnrollment = await _unitOfWork.CourseEnrollments.FindOneAsync(
            //            e => e.StudentId == enrollment.StudentId && e.CourseId == firstCourse.Id,
            //            cancellationToken);

            //        if (courseEnrollment != null && courseEnrollment.Status != EnrollmentStatus.Dropped)
            //        {
            //            courseEnrollment.Status = EnrollmentStatus.InProgress;
            //            await _unitOfWork.CourseEnrollments.UpdateAsync(courseEnrollment, cancellationToken);
            //        }
            //    }
            //}

            if (request.Status == EnrollmentStatus.Completed)
            {
                var student = await _userClient.GetOrganizationUserByIdAsync(enrollment.StudentId, cancellationToken);
                // Publish event curriculum completed
                await _publishEndpoint.Publish(
                    new CurricullumCompletedEvent
                    {
                        Id = enrollment.Id,
                        StudentId = enrollment.StudentId.ToString(),
                        StudentName = student?.FullName ?? "Unknown Student",
                        StudentEmail = student?.Email ?? string.Empty,
                        CurriculumId = enrollment.CurriculumId,
                        CurriculumTitle = curriculum.Title,
                        CompletedAt = enrollment.CompletedAt ?? DateTime.UtcNow,
                    },
                            cancellationToken
                );

                // Create curriculum completion certificate
                //var createCertificateCommand = new CreateCertificateCommand
                //{
                //    UserId = student.UserId,
                //    CertificateType = CertificateType.Curriculum,
                //    CurriculumEnrollmentId = enrollment.Id
                //};
                //await _mediator.Send(createCertificateCommand, cancellationToken);
                await _publishEndpoint.Publish(new CertificateGenerationRequestedEvent
                {
                    UserId = student.UserId,
                    CertificateType = CertificateType.Curriculum.ToString(),
                    CurriculumEnrollmentId = enrollment.Id
                }, cancellationToken);

            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return enrollment.ToEnrollmentModel(curriculum);
        }
    }

}
