using Classroom.Application.Common.Interfaces;
using Classroom.Application.Specifications.Certificates;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Certificates.Queries.GetCertificateList
{
    public class GetCertificateListQueryHandler(IClassroomUnitOfWork unitOfWork)
        : IRequestHandler<GetCertificateListQuery, GrpcPagedCertificatesResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork = unitOfWork;

        public async Task<GrpcPagedCertificatesResponse> Handle(
            GetCertificateListQuery request,
            CancellationToken cancellationToken
        )
        {
            var param = request.CertificateParams;
            var spec = new CertificateSpecification(param);

            var certificates = await _unitOfWork.Certificates.GetAllAsync(spec, cancellationToken);
            var totalCount = await _unitOfWork.Certificates.CountAsync(spec, cancellationToken);

            var grpcCertificates = certificates.Select(c => new GrpcCertificateModel
            {
                Id = c.Id,
                UserId = c.UserId.ToString(),
                CourseEnrollmentId = c.CourseEnrollmentId,
                CurriculumEnrollmentId = c.CurriculumEnrollmentId,
                CertificateType = c.CertificateType.ToString(),

                IssueDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        c.IssueDate
                    ),
                Title = c.Title,
                UserName = c.UserName,
                VerificationCode = c.VerificationCode,
                CertificateUrl = c.CertificateUrl
            });

            return new GrpcPagedCertificatesResponse
            {
                TotalCount = totalCount,
                PageNumber = param.PageNumber,
                PageSize = param.PageSize,
                Items = { grpcCertificates }
            };
        }

        // Helper method for enum mapping
        private Shared.Protos.Classroom.CertificateType MapToProtoCertificateType(Classroom.Domain.Enums.CertificateType type)
        {
            return type switch
            {
                Classroom.Domain.Enums.CertificateType.Course => Shared.Protos.Classroom.CertificateType.Course,
                Classroom.Domain.Enums.CertificateType.Curriculum => Shared.Protos.Classroom.CertificateType.Curriculum,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
