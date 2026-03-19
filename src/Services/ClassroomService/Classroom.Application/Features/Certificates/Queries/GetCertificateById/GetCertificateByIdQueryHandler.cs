using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Specifications.Certificates;
using Classroom.Application.Specifications.CourseEnrollments;
using Classroom.Application.Specifications.CurriculumEnrollments;
using MediatR;
using Shared.Exceptions;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Certificates.Queries.GetCertificateById
{
    public class GetCertificateByIdQueryHandler
        : IRequestHandler<GetCertificateByIdQuery, GrpcCertificateDetail>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcCourseClient _courseClient;

        public GetCertificateByIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcCourseClient courseClient
        )
        {
            _courseClient = courseClient ?? throw new ArgumentNullException(nameof(courseClient));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<GrpcCertificateDetail> Handle(
            GetCertificateByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var certificate = await _unitOfWork.Certificates.FirstOrDefaultAsync(
                new CertificateByIdSpecification(request.Id),
                cancellationToken
            );

            if (certificate == null)
            {
                throw new NotFoundException($"Certificate with ID {request.Id} not found.");
            }

            DateTimeOffset? completedAt = null;
            List<string> lessonTitles = new();
            if (certificate.CourseEnrollmentId != null)
            {
                var courseEnrollmentByIdSpecification = new GetCourseEnrollmentByIdSpecification(certificate.CourseEnrollmentId.Value);
                var courseEnrollment = await _unitOfWork.CourseEnrollments.FirstOrDefaultAsync(
                    courseEnrollmentByIdSpecification,
                    cancellationToken
                );
                if (courseEnrollment == null)
                {
                    throw new NotFoundException($"Course Enrollment with ID {certificate.CourseEnrollmentId.Value} not found.");
                }
                var course = await _courseClient.GetCourseByIdAsync(courseEnrollment.CourseId);
                lessonTitles = course.Lessons.Select(lesson => lesson.Title).ToList();
                completedAt = courseEnrollment?.CompletedAt;
            }
            else if (certificate.CurriculumEnrollmentId != null)
            {
                var curriculumEnrollmentByIdSpecification = new GetCurriculumEnrollmentByIdSpecification(certificate.CurriculumEnrollmentId.Value);
                var curriculumEnrollment = await _unitOfWork.CurriculumEnrollments.FirstOrDefaultAsync(
                    curriculumEnrollmentByIdSpecification,
                    cancellationToken
                );
                completedAt = curriculumEnrollment?.CompletedAt;
            }

            return new GrpcCertificateDetail()
            {
                Id = certificate.Id,
                UserId = certificate.UserId.ToString(),
                CourseEnrollmentId = certificate.CourseEnrollmentId,
                CurriculumEnrollmentId = certificate.CurriculumEnrollmentId,
                CertificateType = certificate.CertificateType.ToString(),
                IssueDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        certificate.IssueDate
                    ),
                VerificationCode = certificate.VerificationCode,
                CertificateUrl = certificate.CertificateUrl,
                UserName = certificate.UserName,
                Title = certificate.Title,
                Lessons = { lessonTitles },
                CompletedAt = completedAt.HasValue
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(completedAt.Value)
                        : null,
            };
        }
    }
}
