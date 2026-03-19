using Google.Protobuf.WellKnownTypes;
using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Models;
using Order.Domain.Entities;
using Shared.Protos.Order;
using System.Linq.Expressions;

namespace Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderList
{
    public class GetOrganizationSubscriptionOrderListQueryHandler
        : IRequestHandler<GetOrganizationSubscriptionOrderListQuery, GrpcPagedOrganizationSubscriptionOrderResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;

        public GetOrganizationSubscriptionOrderListQueryHandler(IOrderUnitOfWork unitOfWork, IPlanBillingCycleCacheService planBillingCycleCacheService)
        {
            _unitOfWork = unitOfWork;
            _planBillingCycleCacheService = planBillingCycleCacheService;
        }

        public async Task<GrpcPagedOrganizationSubscriptionOrderResponse> Handle(
            GetOrganizationSubscriptionOrderListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();

            Expression<Func<OrganizationSubscriptionOrder, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) || c.PlanName.ToLower().Contains(search)) &&
                (!request.Status.HasValue || c.Status == request.Status.Value) &&
                (!request.ContractId.HasValue || c.ContractId == request.ContractId.Value) &&
                (!request.OrganizationId.HasValue || c.OrganizationId == request.OrganizationId.Value) &&
                (!request.PlanBillingCycleId.HasValue || c.PlanBillingCycleId == request.PlanBillingCycleId.Value) &&
                (!request.ParentSubscriptionId.HasValue || c.ParentSubscriptionId == request.ParentSubscriptionId.Value);

            Expression<Func<OrganizationSubscriptionOrder, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.PlanName,
                    _ => c => c.CreatedDate,
                };

            Func<IQueryable<OrganizationSubscriptionOrder>, IQueryable<OrganizationSubscriptionOrderDto>> projectionFunc = query =>
               query
                   .Include(o => o.LicenseAssignments)
                   .Select(o => new OrganizationSubscriptionOrderDto
                   {
                       Id = o.Id,
                       ContractId = o.ContractId,
                       Code = o.Code,
                       OrganizationId = o.OrganizationId,
                       PlanName = o.PlanName,
                       CurriculumCount = o.CurriculumCount,
                       DiscountPercent = o.DiscountPercent,
                       EndDate = o.EndDate,
                       GrossAmount = o.GrossAmount,
                       MaxStudentSeats = o.MaxStudentSeats,
                       MaxTeacherSeats = o.MaxTeacherSeats,
                       NetAmount = o.NetAmount,
                       ParentSubscriptionId = o.ParentSubscriptionId,
                       PlanBillingCycleId = o.PlanBillingCycleId,
                       StartDate = o.StartDate,
                       Status = o.Status,
                       CurrentStudentSeats = o.LicenseAssignments.Count(la =>
                           la.OrganizationSubscriptionOrderId == o.Id &&
                           la.LicenseType == Domain.Enums.LicenseType.Student &&
                           (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)),
                       CurrentTeacherSeats = o.LicenseAssignments.Count(la =>
                           la.OrganizationSubscriptionOrderId == o.Id &&
                           la.LicenseType == Domain.Enums.LicenseType.Teacher &&
                           (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)),
                       CreatedDate = o.CreatedDate,
                       LastModifiedDate = o.LastModifiedDate,
                   });

            var paged = await _unitOfWork.OrganizationSubscriptionOrders.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                projectionFunc: projectionFunc,
                sortExpression: sortExpression,
                predicate: predicate,
                descending: request.IsDescending,
                cancellationToken: cancellationToken
            );

            var dtoItems = paged.Items.ToList();

            // Create tasks for building each response item, then await them all and get real results
            var organizationDetailsTasks = dtoItems.Select(async dto =>
            {
                var plan = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(dto.PlanBillingCycleId, cancellationToken);
                if (plan is not null)
                {
                    dto.PlanBillingCycle = plan.BillingCycle.ToString();
                }

                var detail = new GrpcOrganizationSubscriptionOrderResponse
                {
                    Id = dto.Id,
                    PlanName = dto.PlanName,
                    Code = dto.Code,
                    OrganizationId = dto.OrganizationId,
                    PlanBillingCycleId = dto.PlanBillingCycleId,
                    ContractId = dto.ContractId,
                    ParentSubscriptionId = dto.ParentSubscriptionId ?? 0,
                    GrossAmount = (double)dto.GrossAmount,
                    NetAmount = (double)dto.NetAmount,
                    DiscountPercent = (double)dto.DiscountPercent,
                    StartDate = Timestamp.FromDateTime(dto.StartDate.ToUniversalTime()),
                    EndDate = Timestamp.FromDateTime(dto.EndDate.ToUniversalTime()),
                    MaxStudentSeats = dto.MaxStudentSeats,
                    MaxTeacherSeats = dto.MaxTeacherSeats,
                    CurrentStudentSeats = dto.CurrentStudentSeats,
                    CurrentTeacherSeats = dto.CurrentTeacherSeats,
                    CurriculumCount = dto.CurriculumCount,
                    Status = dto.Status.ToString(),
                    PlanBillingCycle = dto.PlanBillingCycle,
                    CreatedDate = Timestamp.FromDateTimeOffset(dto.CreatedDate),
                    LastModifiedDate = dto.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(dto.LastModifiedDate.Value)
                        : null
                };
                return detail;
            });

            var organizationDetails = await Task.WhenAll(organizationDetailsTasks);

            var response = new GrpcPagedOrganizationSubscriptionOrderResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(organizationDetails);

            return response;
        }
    }
}