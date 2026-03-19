using MediatR;

namespace Classroom.Application.Features.Certificates.Commands.DeleteCertificate
{
    public class DeleteCertificateCommand : IRequest<bool>
    {
        public int CertificateId { get; set; }

        public DeleteCertificateCommand(int certificateId)
        {
            CertificateId = certificateId;
        }
    }
}
