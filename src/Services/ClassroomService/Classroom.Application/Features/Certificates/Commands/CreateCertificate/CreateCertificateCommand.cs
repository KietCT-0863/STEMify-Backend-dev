using Classroom.Application.Models.EnrollmentModels;
using FluentValidation;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Certificates.Commands.CreateCertificate
{
    public class CreateCertificateCommand : IRequest<GrpcCertificateResponse>
    {
        public string UserId { get; set; } = string.Empty;
        public int? CourseEnrollmentId { get; set; }
        public int? CurriculumEnrollmentId { get; set; }
        public Domain.Enums.CertificateType CertificateType { get; set; }
    }

    public class CreateCertificateCommandValidator : AbstractValidator<CreateCertificateCommand>
    {
        public CreateCertificateCommandValidator()
        {
            RuleFor(x => x)
                .Must(cmd => cmd.CertificateType switch
                {
                    Domain.Enums.CertificateType.Course => cmd.CourseEnrollmentId.HasValue && !cmd.CurriculumEnrollmentId.HasValue,
                    Domain.Enums.CertificateType.Curriculum => cmd.CurriculumEnrollmentId.HasValue && !cmd.CourseEnrollmentId.HasValue,
                    _ => false
                })
                .WithMessage("Invalid Enrollment/Curriculum assignment based on CertificateType.");
        }
    }
}
