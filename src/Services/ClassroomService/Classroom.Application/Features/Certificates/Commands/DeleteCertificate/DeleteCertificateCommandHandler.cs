using Classroom.Application.Common.Interfaces;
using MediatR;
using Shared.Exceptions;

namespace Classroom.Application.Features.Certificates.Commands.DeleteCertificate
{
    public class DeleteCertificateCommandHandler : IRequestHandler<DeleteCertificateCommand, bool>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;

        public DeleteCertificateCommandHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(
            DeleteCertificateCommand request,
            CancellationToken cancellationToken
        )
        {
            var certificate = await _unitOfWork.Certificates.FindByIdAsync(
                request.CertificateId,
                cancellationToken
            );
            if (certificate == null)
            {
                throw new NotFoundException($"Certificate with ID {request.CertificateId} not found.");
            }

            await _unitOfWork.Certificates.DeleteAsync(certificate, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0;
        }
    }
}
