using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Cache;
using Classroom.Application.Common.Interfaces.Grpc;
using Contracts.Abstractions.Services;
using EventBus.Messages;
using MassTransit;
using MediatR;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Classroom;
using System.Security.Cryptography;
using System.Text;

namespace Classroom.Application.Features.Certificates.Commands.CreateCertificate
{
    public class CreateCertificateCommandHandler
        : IRequestHandler<CreateCertificateCommand, GrpcCertificateResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ICourseCacheService _courseCache;
        private readonly ICurriculumCacheService _curriculumCache;
        private readonly IGrpcUserClient _userClient;
        private readonly IPdfService _pdfService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateCertificateCommandHandler(
            IClassroomUnitOfWork unitOfWork,
            ICurriculumCacheService curriculumCache,
            IGrpcUserClient userClient,
            IPdfService pdfService,
            ICloudinaryService cloudinaryService,
            IPublishEndpoint publishEndpoint,
            ICourseCacheService courseCache
        )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _curriculumCache = curriculumCache ?? throw new ArgumentNullException(nameof(curriculumCache));
            _courseCache = courseCache ?? throw new ArgumentNullException(nameof(courseCache));
            _userClient = userClient ?? throw new ArgumentNullException(nameof(userClient));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        public async Task<GrpcCertificateResponse> Handle(
            CreateCertificateCommand request,
            CancellationToken cancellationToken
        )
        {
            int? enrollmentId = null;
            string certificateName;
            string code;
            string description;

            var user = await _userClient.GetUserByIdAsync(Guid.Parse(request.UserId));
            if (user == null)
                throw new ArgumentException("User not found.");

            if (request.CertificateType == Domain.Enums.CertificateType.Course)
            {
                if (!request.CourseEnrollmentId.HasValue)
                    throw new ArgumentException("EnrollmentId is required for course certificate.");

                var enrollment = await _unitOfWork.CourseEnrollments
                    .FindByIdAsync(request.CourseEnrollmentId.Value, cancellationToken);
                if (enrollment == null)
                    throw new ArgumentException("Course enrollment not found.");

                enrollmentId = enrollment.Id;
                var course = await _courseCache.GetByIdAsync(enrollment.CourseId, cancellationToken);
                certificateName = course?.Title ?? "Course Certificate";
                description = course?.Description ?? "Course Certificate";
                code = course?.Code ?? "COURSE";
            }
            else if (request.CertificateType == Domain.Enums.CertificateType.Curriculum)
            {
                if (!request.CurriculumEnrollmentId.HasValue)
                    throw new ArgumentException("CurriculumId is required for curriculum certificate.");

                var curriculumEnrollment = await _unitOfWork.CurriculumEnrollments
                    .FindByIdAsync(request.CurriculumEnrollmentId.Value, cancellationToken);
                if (curriculumEnrollment == null)
                    throw new ArgumentException("Curriculum enrollment not found.");

                enrollmentId = curriculumEnrollment.Id;
                var curriculum = await _curriculumCache.GetByIdAsync(curriculumEnrollment.CurriculumId, cancellationToken);
                certificateName = curriculum?.Title ?? "Curriculum Certificate";
                description = curriculum?.Description ?? "Curriculum Certificate";
                code = curriculum?.Code ?? "CURR";
            }
            else
            {
                throw new ArgumentException("Invalid certificate type.");
            }

            var issuedAt = DateTime.UtcNow;

            var verificationCode = GenerateVerificationCode(
                request.CertificateType,
                request.UserId,
                code,
                issuedAt
            );

            // Create certificate entity
            var certificate = new Domain.Entities.Certificate
            {
                UserId = Guid.Parse(request.UserId),
                UserName = user.Name,
                Title = certificateName,
                VerificationCode = verificationCode,
                CertificateType = request.CertificateType,
                IssueDate = issuedAt
            };

            if (request.CertificateType == Domain.Enums.CertificateType.Course)
            {
                certificate.CourseEnrollmentId = enrollmentId;
            }
            else if (request.CertificateType == Domain.Enums.CertificateType.Curriculum)
            {
                certificate.CurriculumEnrollmentId = enrollmentId;
            }

            var templatePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "courseCer.html"
            );
            var htmlContent = await System.IO.File.ReadAllTextAsync(templatePath);

            // Replace placeholders with actual data
            htmlContent = htmlContent
                .Replace("{{recipientName}}", certificate.UserName)
                .Replace("{{courseTitle}}", certificate.Title)
                .Replace("{{courseDescription}}", description)
                .Replace("{{issueDate}}", certificate.IssueDate.ToString("dd-MM-yyyy"))
                .Replace("{{verificationCode}}", certificate.VerificationCode);

            // Generate PDF and upload to Cloudinary
            var pdfBytes = await _pdfService.ConvertHtmlToPdfAsync(htmlContent);

            var uploadDocumentBytesRequest = new UploadDocumentBytesRequest()
            {
                FileBytes = pdfBytes,
                FileName = $"certificate_{certificate.VerificationCode}.pdf"
            };

            var uploadResponse = await _cloudinaryService.UploadDocumentAsync(
                uploadDocumentBytesRequest
            );

            certificate.CertificateUrl = uploadResponse.AssetUrl;

            await _unitOfWork.Certificates.AddAsync(certificate, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _publishEndpoint.Publish(
                new CertificateCreatedEvent
                {
                    StudentId = user.UserId,
                    Name = user?.Name ?? "Unknown Student",
                    Email = user?.Email ?? string.Empty,
                    CertificateTitile = certificate.Title,
                    CertificateUrl = certificate.CertificateUrl,
                    CertificateType = request.CertificateType.ToString(),
                    Code = certificate.VerificationCode
                },
                    cancellationToken
                );

            return new GrpcCertificateResponse
            {
                Certificate = new GrpcCertificateModel
                {
                    Id = certificate.Id,
                    UserId = certificate.UserId.ToString(),
                    CourseEnrollmentId = certificate.CourseEnrollmentId,
                    CurriculumEnrollmentId = certificate.CurriculumEnrollmentId,
                    CertificateType = request.CertificateType.ToString(),
                    IssueDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        certificate.IssueDate
                    ),
                    VerificationCode = certificate.VerificationCode,
                    CertificateUrl = certificate.CertificateUrl,
                    UserName = certificate.UserName,
                    Title = certificate.Title
                }
            };
        }

        private string GenerateVerificationCode(
            Domain.Enums.CertificateType type,
            string userId,
            string code,
            DateTime issuedAt)
        {
            const string base62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

            // Combine inputs into a unique string
            var raw = $"{type}-{userId}-{issuedAt:yyyyMMddHHmmss}-{code}";

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

            // Take first 8 bytes (64 bits)
            ulong value = BitConverter.ToUInt64(hashBytes, 0);

            // Convert to Base62
            var sb = new StringBuilder();
            while (value > 0)
            {
                sb.Insert(0, base62[(int)(value % 62)]);
                value /= 62;
            }

            var prefix = type switch
            {
                Domain.Enums.CertificateType.Course => "C",
                Domain.Enums.CertificateType.Curriculum => "CR",
                _ => "X"
            };

            return $"{prefix}-{sb.ToString().PadLeft(12, '0')}";
        }

    }
}