using Google.Protobuf.WellKnownTypes;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Specifications;
using Shared.Protos.Order;
using Shared.Protos.User;

namespace Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentById
{
    public class GetLicenseAssignmentByIdQueryHandler
        : IRequestHandler<GetLicenseAssignmentByIdQuery, GrpcLicenseAssignmentDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _grpcUserClient;

        public GetLicenseAssignmentByIdQueryHandler(IOrderUnitOfWork unitOfWork, IGrpcUserClient grpcUserClient)
        {
            _unitOfWork = unitOfWork;
            _grpcUserClient = grpcUserClient;
        }

        public async Task<GrpcLicenseAssignmentDetail> Handle(
            GetLicenseAssignmentByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new LicenseAssignmentByIdSpecification(request.Id);

            var licenseAssignment = await _unitOfWork.LicenseAssignments.FirstOrDefaultAsync(spec, cancellationToken);

            if (licenseAssignment == null)
            {
                throw new KeyNotFoundException($"LicenseAssignment with ID {request.Id} not found.");
            }

            OrganizationUserInfo? user = null;
            if (Guid.TryParse(licenseAssignment.OrganizationUserId, out var userGuid))
            {
                user = await _grpcUserClient.GetOrganizationUserByIdAsync(userGuid, cancellationToken);
            }
            GrpcUserResponse grpcUserResponse = user != null
                ? new GrpcUserResponse
                {
                    UserId = user.OrganizationUserId,
                    UserName = user.FullName,
                    Email = user.Email,
                    ImageUrl = ""
                }
                : new GrpcUserResponse
                {
                    UserId = licenseAssignment.OrganizationUserId,
                    UserName = "Unknown",
                    Email = "Unknown",
                    ImageUrl = ""
                };

            var grpcLicenseAssignment = new GrpcLicenseAssignmentDetail
            {
                Id = licenseAssignment.Id,
                OrganizationSubscriptionOrderId = licenseAssignment.OrganizationSubscriptionOrderId,
                OrganizationUserId = licenseAssignment.OrganizationUserId,
                Status = licenseAssignment.Status.ToString(),
                Type = licenseAssignment.LicenseType.ToString(),
                AssignedAt = Timestamp.FromDateTimeOffset(new DateTimeOffset(licenseAssignment.AssignedAt)),
                RevokedAt = licenseAssignment.RevokedAt.HasValue
                        ? Timestamp.FromDateTimeOffset(new DateTimeOffset(licenseAssignment.RevokedAt.Value))
                        : null,
                User = grpcUserResponse
            };

            return grpcLicenseAssignment;
        }
    }
}