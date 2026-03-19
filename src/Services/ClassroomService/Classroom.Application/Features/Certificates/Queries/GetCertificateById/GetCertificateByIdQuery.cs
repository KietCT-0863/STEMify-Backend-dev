using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Certificates.Queries.GetCertificateById
{
    public class GetCertificateByIdQuery(int id) : IRequest<GrpcCertificateDetail>
    {
        public int Id { get; } = id;
    }
}
