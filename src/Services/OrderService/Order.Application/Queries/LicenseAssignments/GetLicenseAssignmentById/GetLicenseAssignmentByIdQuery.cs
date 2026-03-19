using MediatR;
using Shared.Protos.Order;

namespace Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentById
{
    public class GetLicenseAssignmentByIdQuery : IRequest<GrpcLicenseAssignmentDetail>
    {
        public int Id { get; set; }
    }
}
