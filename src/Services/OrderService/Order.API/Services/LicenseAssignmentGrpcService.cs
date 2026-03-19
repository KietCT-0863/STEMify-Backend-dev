using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Application.Commands.LicenseAssignments.ActivateReservedLicense;
using Order.Application.Commands.LicenseAssignments.AssignLicenseByEmail;
using Order.Application.Commands.LicenseAssignments.CreateLicenseAssignment;
using Order.Application.Commands.LicenseAssignments.DeleteLicenseAssignment;
using Order.Application.Commands.LicenseAssignments.ReserveLicenseByEmail;
using Order.Application.Commands.LicenseAssignments.UpdateLicenseAssignment;
using Order.Application.Queries.LicenseAssignments.BulkCheckLicenses;
using Order.Application.Queries.LicenseAssignments.CheckLicenseAvailability;
using Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentById;
using Order.Application.Queries.LicenseAssignments.GetLicenseAssignmentList;
using ServiceStack;
using Shared.Enums;
using Shared.Protos.Order;

namespace Order.API.Services
{
    public class LicenseAssignmentGrpcService : GrpcLicenseAssignmentService.GrpcLicenseAssignmentServiceBase
    {
        private readonly IMediator _mediator;

        public LicenseAssignmentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcLicenseAssignmentListModel> CreateLicenseAssignment(
            CreateLicenseAssignmentsRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateLicenseAssignmentCommand
            {
                LicenseAssignments = request.LicenseAssignments
                    .Select(la => new CreateLicenseAssignmentModel
                    {
                        Type = la.Type.ToEnumOrDefault(Domain.Enums.LicenseType.Student),
                        OrganizationSubscriptionOrderId = la.OrganizationSubscriptionOrderId,
                        UserId = la.UserId
                    })
                    .ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcLicenseAssignmentDetail> GetLicenseAssignmentById(
            GetLicenseAssignmentRequest request,
            ServerCallContext context
        )
        {
            var query = new GetLicenseAssignmentByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GrpcLicenseAssignmentResponse> UpdateLicenseAssignment(
            UpdateLicenseAssignmentRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateLicenseAssignmentCommand
            {
                Id = request.Id,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.LicenseAssignmentStatus.Active)
            };

            var result = await _mediator.Send(command);
            return new GrpcLicenseAssignmentResponse { LicenseAssignment = result };
        }

        public override async Task<Empty> DeleteLicenseAssignment(
            DeleteLicenseAssignmentRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteLicenseAssignmentCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedLicenseAssignmentResponse> GetPagedLicenseAssignment(
            GetLicenseAssignmentParams request,
            ServerCallContext context
        )
        {
            var query = new GetLicenseAssignmentListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                OrganizationSubscriptionOrderId = request.OrganizationSubscriptionOrderId,
                UserId = request.UserId,
                Type = string.IsNullOrEmpty(request.Type) ? null : request.Type.ToEnumOrDefault(Domain.Enums.LicenseType.Student),
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.LicenseAssignmentStatus.Active)
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<CheckLicenseAvailabilityResponse> CheckLicenseAvailability(
            CheckLicenseAvailabilityRequest request,
            ServerCallContext context)
        {
            var query = new CheckLicenseAvailabilityQuery
            {
                OrganizationId = request.OrganizationId,
                LicenseType = request.LicenseType,
                RequestedCount = request.RequestedCount
            };

            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<BulkCheckLicensesResponse> BulkCheckLicenses(
            BulkCheckLicensesRequest request,
            ServerCallContext context)
        {
            var query = new BulkCheckLicensesQuery
            {
                OrganizationId = request.OrganizationId,
                LicenseRequests = request.LicenseRequests.ToList()
            };

            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<AssignLicenseByEmailResponse> AssignLicenseByEmail(
            AssignLicenseByEmailRequest request,
            ServerCallContext context)
        {
            var command = new AssignLicenseByEmailCommand
            {
                OrganizationId = request.OrganizationId,
                UserEmail = request.UserEmail,
                LicenseType = request.LicenseType,
                SubscriptionOrderId = request.SubscriptionOrderId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<ReserveLicenseByEmailResponse> ReserveLicenseByEmail(
            ReserveLicenseByEmailRequest request,
            ServerCallContext context)
        {
            var command = new ReserveLicenseByEmailCommand
            {
                OrganizationId = request.OrganizationId,
                OrganizationUserId = request.OrganizationUserId,
                LicenseType = request.LicenseType,
                SubscriptionOrderId = request.SubscriptionOrderId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<ActivateReservedLicenseResponse> ActivateReservedLicense(
            ActivateReservedLicenseRequest request,
            ServerCallContext context)
        {
            var command = new ActivateReservedLicenseCommand
            {
                OrganizationId = request.OrganizationId,
                OrganizationUserId = request.OrganizationUserId,
                LicenseType = request.LicenseType,
                SubscriptionOrderId = request.SubscriptionOrderId
            };

            var result = await _mediator.Send(command);
            return result;
        }
    }
}