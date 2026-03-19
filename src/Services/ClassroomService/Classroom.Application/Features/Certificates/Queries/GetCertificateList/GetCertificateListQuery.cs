using Classroom.Application.Specifications.Certificates;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Certificates.Queries.GetCertificateList
{
    public class GetCertificateListQuery(CertificateParams certificateParams)
        : IRequest<GrpcPagedCertificatesResponse>
    {
        public CertificateParams CertificateParams { get; set; } = certificateParams;
    }
}
