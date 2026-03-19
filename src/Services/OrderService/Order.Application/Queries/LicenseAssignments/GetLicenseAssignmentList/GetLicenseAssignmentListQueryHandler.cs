using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Grpc;
using Order.Domain.Entities;
using Shared.Protos.Order;
using Shared.Protos.User;
using System.Linq.Expressions;

namespace Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentList
{
    public class GetLicenseAssignmentListQueryHandler
        : IRequestHandler<GetLicenseAssignmentListQuery, GrpcPagedLicenseAssignmentResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcUserClient _grpcUserClient;

        public GetLicenseAssignmentListQueryHandler(
            IOrderUnitOfWork unitOfWork, 
            IGrpcUserClient grpcUserClient)
        {
            _unitOfWork = unitOfWork;
            _grpcUserClient = grpcUserClient;
        }

        public async Task<GrpcPagedLicenseAssignmentResponse> Handle(
            GetLicenseAssignmentListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();

            List<string>? organizationUserIdsForUser = null;
            if (!string.IsNullOrWhiteSpace(request.UserId) &&
                Guid.TryParse(request.UserId, out var userId))
            {
                var orgUsers = await _grpcUserClient.GetOrganizationUsersByUserIdAsync(
                    userId,
                    cancellationToken);

                organizationUserIdsForUser = orgUsers.Items
                    .Select(u => u.OrganizationUserId)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();
            }

            Expression<Func<LicenseAssignment, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) || c.OrganizationUserId.Contains(search)) &&
                (!request.Status.HasValue || c.Status == request.Status.Value) &&
                (!request.Type.HasValue || c.LicenseType == request.Type.Value) &&
                (organizationUserIdsForUser == null ||
                 organizationUserIdsForUser.Contains(c.OrganizationUserId)) &&
                (!request.OrganizationSubscriptionOrderId.HasValue || c.OrganizationSubscriptionOrderId == request.OrganizationSubscriptionOrderId.Value);

            Expression<Func<LicenseAssignment, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "status" => c => c.Status,
                    "type" => c => c.LicenseType,
                    _ => c => c.AssignedAt,
                };

            Func<IQueryable<LicenseAssignment>, IQueryable<LicenseAssignment>> projectionFunc = query =>
               query
                   .Include(q => q.OrganizationSubscriptionOrder)
                   .ThenInclude(o => o.Organization);

            var paged = await _unitOfWork.LicenseAssignments.GetByPageFilter(
                projectionFunc: projectionFunc,
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                sortExpression: sortExpression,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new GrpcPagedLicenseAssignmentResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var item in paged.Items)
            {
                GrpcUserResponse? user = null;
                if (Guid.TryParse(item.OrganizationUserId, out var organizationUserId))
                {

                    var orgUserResponse = await _grpcUserClient.GetOrganizationUserByIdAsync(
                        organizationUserId, 
                        cancellationToken);
                    user = new GrpcUserResponse
                    {
                        UserId = orgUserResponse.UserId,
                        Name = orgUserResponse.FullName,
                        Email = orgUserResponse.Email,
                        ImageUrl = "",
                    };
                }

                var licenseAssignment = new GrpcLicenseAssignmentDetail
                {
                    Id = item.Id,
                    OrganizationSubscriptionOrderId = item.OrganizationSubscriptionOrderId,
                    OrganizationId = item.OrganizationSubscriptionOrder.OrganizationId,
                    OrganizationImageUrl = item.OrganizationSubscriptionOrder.Organization.ImageUrl,
                    OrganizationName = item.OrganizationSubscriptionOrder.Organization.Name,
                    PlanName = item.OrganizationSubscriptionOrder.PlanName,
                    OrganizationUserId = item.OrganizationUserId,
                    Status = item.Status.ToString(),
                    Type = item.LicenseType.ToString(),
                    AssignedAt = Timestamp.FromDateTimeOffset(item.AssignedAt),
                    RevokedAt = item.RevokedAt.HasValue
                        ? Timestamp.FromDateTimeOffset(item.RevokedAt.Value)
                        : null,
                    User = user
                };
                response.Items.Add(licenseAssignment);
            }

            return response;
        }
    }
}