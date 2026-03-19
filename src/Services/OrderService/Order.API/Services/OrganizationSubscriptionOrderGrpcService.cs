using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Application.Commands.OrganizationSubscriptionOrders.CancelOrganizationSubscriptionOrder;
using Order.Application.Commands.OrganizationSubscriptionOrders.CreateOrganizationSubscriptionOrder;
using Order.Application.Commands.OrganizationSubscriptionOrders.DeleteOrganizationSubscriptionOrder;
using Order.Application.Commands.OrganizationSubscriptionOrders.UpdateOrganizationSubscriptionOrder;
using Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderById;
using Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderList;
using ServiceStack;
using Shared.Enums;
using Shared.Protos.Order;
using System.Globalization;

namespace Order.API.Services
{
    public class OrganizationSubscriptionOrderGrpcService : GrpcOrganizationSubscriptionOrderService.GrpcOrganizationSubscriptionOrderServiceBase
    {
        private readonly IMediator _mediator;

        public OrganizationSubscriptionOrderGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcOrganizationSubscriptionOrderDetail> CreateOrganizationSubscriptionOrder(
            CreateOrganizationSubscriptionOrderRequest request,
            ServerCallContext context
        )
        {
            // Validate and parse date-only strings (expected yyyy-MM-dd)
            var start = ParseDateOnlyOrThrow(request.StartDate, nameof(request.StartDate));

            var command = new CreateOrganizationSubscriptionOrderCommand
            {
                ContractId = request.ContractId,
                OrganizationId = request.OrganizationId,
                PlanBillingCycleId = request.PlanBillingCycleId,
                ParentSubscriptionId = request.ParentSubscriptionId,
                CurriculumIds = request.CurriculumIds.ToList(),
                DiscountPercent = (decimal)request.DiscountPercent,
                MaxStudentSeats = request.MaxStudentSeats,
                MaxTeacherSeats = request.MaxTeacherSeats,
                StartDate = start,
                Contract = request.Contract != null ? new CreateContractDto
                {
                    Name = request.Contract.Name,
                    Description = request.Contract.Description,
                    FileBytes = request.Contract.File?.ToByteArray(),
                    OrganizationId = request.OrganizationId
                } : null
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcOrganizationSubscriptionOrderDetail> GetOrganizationSubscriptionOrderById(
            GetOrganizationSubscriptionOrderRequest request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationSubscriptionOrderByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<Empty> CancelOrganizationSubscriptionOrder(
            CancelOrganizationSubscriptionOrderRequest request,
            ServerCallContext context
        )
        {
            var command = new CancelOrganizationSubscriptionOrderCommand
            {
                Id = request.Id
            };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcOrganizationSubscriptionOrderDetail> UpdateOrganizationSubscriptionOrder(
            UpdateOrganizationSubscriptionOrderRequest request,
            ServerCallContext context
        )
        {
            DateTime? start = null;

            if (!string.IsNullOrWhiteSpace(request.StartDate))
            {
                start = TryParseDateOnly(request.StartDate);
                if (!start.HasValue)
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid StartDate value: {request.StartDate}. Expected format: yyyy-MM-dd"));
            }

            var command = new UpdateOrganizationSubscriptionOrderCommand
            {
                Id = request.Id,
                //NetAmount = request.NetAmount.HasValue ? (decimal?)request.NetAmount.Value : null,
                //GrossAmount = request.GrossAmount.HasValue ? (decimal?)request.GrossAmount.Value : null,
                DiscountPercent = request.DiscountPercent.HasValue ? (decimal?)request.DiscountPercent.Value : null,
                MaxStudentSeats = request.MaxStudentSeats,
                MaxTeacherSeats = request.MaxTeacherSeats,
                CurriculumIds = request.CurriculumIds?.ToList() ?? new List<int>(),
                StartDate = start,
                //PlanName = string.IsNullOrEmpty(request.PlanName) ? null : request.PlanName,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteOrganizationSubscriptionOrder(
            DeleteOrganizationSubscriptionOrderRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteOrganizationSubscriptionOrderCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedOrganizationSubscriptionOrderResponse> GetPagedOrganizationSubscriptionOrder(
            GetOrganizationSubscriptionOrderParams request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationSubscriptionOrderListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                ContractId = request.ContractId,
                OrganizationId = request.OrganizationId,
                ParentSubscriptionId = request.ParentSubscriptionId,
                PlanBillingCycleId = request.PlanBillingCycleId,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.OrganizationSubscriptionOrderStatus.Active)
            };
            var result = await _mediator.Send(query);

            return result;
        }

        private static DateTime ParseDateOnlyOrThrow(string? dateStr, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"{fieldName} is required and must be in yyyy-MM-dd format."));

            // Accept exactly "yyyy-MM-dd" (date-only). Adjust formats array if you want to accept more.
            if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                // Convert to UTC midnight
                return d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            }

            // Try ISO parse as fallback (e.g. "2025-10-25T00:00:00Z")
            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            {
                return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
            }

            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid {fieldName} value: {dateStr}. Expected format: yyyy-MM-dd"));
        }

        private static DateTime? TryParseDateOnly(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr))
                return null;

            if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);

            return null;
        }
    }
}