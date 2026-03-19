using Classroom.Application.Features.CurriculumEnrollments.Commands.CreateCurriculumEnrollment;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CurriculumEnrollments;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Shared.Extensions;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.Application.Extensions.Mapping
{
    public static class CurriculumEnrollmentMappingExtension
    {
        // Mapping extension to convert CreateEnrollmentCommand to CurriculumEnrollment entity
        public static CurriculumEnrollment ToEnrollmentEntity(this CreateCurriculumEnrollmentCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            return new CurriculumEnrollment { StudentId = command.StudentId, CurriculumId = command.CurriculumId};
        }

        // Mapping extension to convert CurriculumEnrollment entity to CurriculumEnrollmentModel
        public static CurriculumEnrollmentModel ToEnrollmentModel(
            this CurriculumEnrollment enrollment,
            CurriculumModel? curriculum
        )
        {
            if (enrollment == null)
                throw new ArgumentNullException(nameof(enrollment));
            return new CurriculumEnrollmentModel
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId.ToString(),
                CurriculumId = enrollment.CurriculumId,
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt,
                Status = enrollment.Status.ToString(),
                CurriculumTitle = curriculum?.Title ?? String.Empty,
                CoverImageUrl = curriculum?.ImageUrl,
                Description = curriculum?.Description,
                ProgressPercentage = enrollment.ProgressPercentage,
            };
        }

        // Mapping extension to convert Grpc request to domain params
        public static CurriculumEnrollmentParams ToEnrollmentParams(this GetCurriculumEnrollmentsRequest request)
        {
            return new CurriculumEnrollmentParams
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                OrderBy = request.OrderBy,
                StudentId =
                    string.IsNullOrWhiteSpace(request.StudentId) ? null
                    : Guid.TryParse(request.StudentId, out var guid) ? guid
                    : throw new FormatException("StudentId is not a valid GUID"),
                CurriculumId = request.CurriculumId,
                CertificateId = request.CertificateId,
                OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId,
                VerificationCode = request.VerificationCode,
                Status = !string.IsNullOrWhiteSpace(request.Status)
                        ? System.Enum.Parse<EnrollmentStatus>(
                            request.Status) : null,
            };
        }

        // mapping extension to convert CreateEnrollmentRequest to CreateEnrollmentCommand
        public static CreateCurriculumEnrollmentCommand ToCreatEnrollmentCommand(
            this CreateCurriculumEnrollmentRequest request
        )
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return new CreateCurriculumEnrollmentCommand
            {
                StudentId = Guid.Parse(request.StudentId),
                CurriculumId = request.CurriculumId,
                Status = request.Status.ToEnumOrNull<EnrollmentStatus>() ?? EnrollmentStatus.InProgress,
                ClassroomId = request.ClassroomId,
            };
        }

        // Mapping extension to convert CurriculumEnrollmentModel to Grpc model
        public static GrpcCurriculumEnrollmentModel ToGrpcEnrollmentModel(this CurriculumEnrollmentModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            return new GrpcCurriculumEnrollmentModel
            {
                Id = model.Id,
                StudentId = model.StudentId.ToString(),
                CurriculumId = model.CurriculumId,
                EnrolledAt = model.EnrolledAt.ToUtcTimestamp(),
                CompletedAt = model.CompletedAt?.ToUtcTimestamp(),
                Status = model.Status,
                CurriculumTitle = model.CurriculumTitle,
                CoverImageUrl = model.CoverImageUrl,
                Description = model.Description,
                ProgressPercentage = model.ProgressPercentage,
            };
        }

        public static GrpcCurriculumEnrollmentDetail ToGrpcEnrollmentDetail(this CurriculumEnrollmentModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            return new GrpcCurriculumEnrollmentDetail
            {
                Id = model.Id,
                StudentId = model.StudentId.ToString(),
                StudentName = model.StudentName,
                CurriculumId = model.CurriculumId,
                EnrolledAt = model.EnrolledAt.ToUtcTimestamp(),
                ProgressPercentage = model.ProgressPercentage,
                CompletedAt = model.CompletedAt?.ToUtcTimestamp(),
                Status = model.Status,
                CurriculumTitle = model.CurriculumTitle,
                CoverImageUrl = model.CoverImageUrl,
                Description = model.Description,
                CertificateUrl = model.CertificateUrl,
                VerificationCode = model.VerificationCode,
                CertificateId = model.CertificateId,
                CourseEnrollments = { model.CourseEnrollments.Select(ce => ce.ToGrpcEnrollmentModel()) },
            };
        }
    }
}
