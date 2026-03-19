using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcCertificateClient
    {
        Task<GrpcPagedCertificatesResponse> GetPagedCertificates(GetCertificatesRequest request);
    }
}
