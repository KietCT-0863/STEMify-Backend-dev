using Classroom.Application.Features.CourseEnrollments.Commands.CreateCourseEnrollment;
using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Models.EnrollmentModels;
using Classroom.Application.Specifications.CourseEnrollments;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;
using Shared.Extensions;
using Shared.Helper;
using Shared.Protos.Classroom;

namespace Classroom.Application.Extensions.Mapping
{
    public static class CourseEnrollmentMappingExtension
    {
        // Mapping extension to convert CreateEnrollmentCommand to CourseEnrollment entity
        public static CourseEnrollment ToEnrollmentEntity(this CreateCourseEnrollmentCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            return new CourseEnrollment
            {
                StudentId = command.StudentId,
                CourseId = command.CourseId,
                Status = command.Status,
                ClassroomId = command.ClassroomId,
                CurriculumEnrollmentId = command.CurriculumEnrollmentId
            };
        }

        // Mapping extension to convert CourseEnrollment entity to CourseEnrollmentModel
        public static CourseEnrollmentModel ToEnrollmentModel(
            this CourseEnrollment enrollment,
            CourseModel? course = null,
            Certificate? certificate = null
        )
        {
            if (enrollment == null)
                throw new ArgumentNullException(nameof(enrollment));
            return new CourseEnrollmentModel
            {
                Id = enrollment.Id,
                ClassroomId = enrollment.ClassroomId,
                StudentId = enrollment.StudentId.ToString(),
                CourseId = enrollment.CourseId,
                ProgressPercentage = enrollment.ProgressPercentage,
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt,
                Status = enrollment.Status.ToString(),
                CourseTitle = course?.Title ?? String.Empty,
                CoverImageUrl = course?.ImageUrl,
                Description = course?.Description,
                Duration = course?.Duration ?? 0,
                AgeRangeLabel = course?.AgeRangeLabel ?? String.Empty,
                FinalScore = enrollment.FinalScore,
                VerificationCode = certificate?.VerificationCode,
                CertificateUrl = certificate?.CertificateUrl,
                CertificateId = certificate?.Id
            };
        }

        // Mapping extension to convert Grpc request to domain params
        public static CourseEnrollmentParams ToEnrollmentParams(this GetCourseEnrollmentsRequest request)
        {
            return new CourseEnrollmentParams
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                OrderBy = request.OrderBy,
                ClassroomId = request.ClassroomId,
                StudentId =
                    string.IsNullOrWhiteSpace(request.StudentId) ? null
                    : Guid.TryParse(request.StudentId, out var guid) ? guid
                    : throw new FormatException("StudentId is not a valid GUID"),
                CourseId = request.CourseId,
                VerificationCode = request.VerificationCode,
                Status = !string.IsNullOrWhiteSpace(request.Status)
                        ? System.Enum.Parse<EnrollmentStatus>(
                            request.Status) : null,
            };
        }

        // mapping extension to convert CreateEnrollmentRequest to CreateEnrollmentCommand
        public static CreateCourseEnrollmentCommand ToCreatEnrollmentCommand(
            this CreateCourseEnrollmentRequest request
        )
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return new CreateCourseEnrollmentCommand
            {
                StudentId = Guid.Parse(request.StudentId),
                CourseId = request.CourseId,
                Status = request.Status.ToEnumOrNull<EnrollmentStatus>()
                         ?? EnrollmentStatus.InProgress,
                CurriculumEnrollmentId = request.CurriculumEnrollmentId,
                ClassroomId = request.ClassroomId,
            };
        }

        // Mapping extension to convert CourseEnrollmentModel to Grpc model
        public static GrpcCourseEnrollmentModel ToGrpcEnrollmentModel(this CourseEnrollmentModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            return new GrpcCourseEnrollmentModel
            {
                Id = model.Id,
                StudentId = model.StudentId.ToString(),
                StudentName = model.StudentName,
                CourseId = model.CourseId,
                EnrolledAt = model.EnrolledAt?.ToUtcTimestamp(),
                CompletedAt = model.CompletedAt?.ToUtcTimestamp(),
                Status = model.Status,
                CourseTitle = model.CourseTitle,
                CoverImageUrl = model.CoverImageUrl,
                Description = model.Description,
                Duration = model.Duration ?? 0,
                AgeRangeLabel = model.AgeRangeLabel ?? "",
                FinalScore = model.FinalScore,
                CertificateUrl = model.CertificateUrl,
                VerificationCode = model.VerificationCode,
                ProgressPercentage = model.ProgressPercentage,
                CertificateId = model.CertificateId,
                ClassroomId = model.ClassroomId,
            };
        }
    }
}
