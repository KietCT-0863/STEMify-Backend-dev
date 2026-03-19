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

namespace Order.Application.Queries.Organizations.GetOrganizationList
{
    public class GetOrganizationListQueryHandler
        : IRequestHandler<GetOrganizationListQuery, GrpcPagedOrganizationResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;

        public GetOrganizationListQueryHandler(IOrderUnitOfWork unitOfWork, IPlanBillingCycleCacheService planBillingCycleCacheService)
        {
            _unitOfWork = unitOfWork;
            _planBillingCycleCacheService = planBillingCycleCacheService;
        }

        public async Task<GrpcPagedOrganizationResponse> Handle(
            GetOrganizationListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();

            Expression<Func<Organization, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) || c.Name.ToLower().Contains(search)) &&
                (!request.Status.HasValue || c.Status == request.Status.Value) &&
                (!request.OrganizationTypeId.HasValue || c.OrganizationTypeId == request.OrganizationTypeId.Value)
                ;

            Expression<Func<Organization, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    "createddate" => c => c.CreatedDate,
                    _ => c => c.Name,
                };

            Func<IQueryable<Organization>, IQueryable<OrganizationDto>> projectionFunc = query =>
               query
                   .Include(organization => organization.OrganizationType)
                   .Include(organization => organization.SubscriptionOrders)
                        .ThenInclude(subscriptionOrders => subscriptionOrders.LicenseAssignments)
                   .Select(organization => new OrganizationDto
                   {
                       Id = organization.Id,
                       Name = organization.Name,
                       Code = organization.Code,
                       Description = organization.Description ?? string.Empty,
                       ImageUrl = organization.ImageUrl,
                       OrganizationType = organization.OrganizationType.Name,
                       Status = organization.Status,
                       CreatedDate = organization.CreatedDate,
                       LastModifiedDate = organization.LastModifiedDate,
                       Subscriptions = organization.SubscriptionOrders.Select(order => new SubscriptionDto
                       {
                           Id = order.Id,
                           PlanBillingCycleId = order.PlanBillingCycleId,
                           CurrentStudentSeats = order.LicenseAssignments.Count(la =>
                                la.OrganizationSubscriptionOrderId == organization.Id &&
                                la.LicenseType == Domain.Enums.LicenseType.Student &&
                                (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)),
                           CurrentTeacherSeats = order.LicenseAssignments.Count(la =>
                                la.OrganizationSubscriptionOrderId == organization.Id &&
                                la.LicenseType == Domain.Enums.LicenseType.Teacher &&
                                (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)),
                           StartDate = order.StartDate,
                           EndDate = order.EndDate,
                           GrossAmount = order.GrossAmount,
                           MaxStudentSeats = order.MaxStudentSeats,
                           MaxTeacherSeats = order.MaxTeacherSeats,
                           NetAmount = order.NetAmount,
                           PlanBillingCycle = string.Empty,
                           PlanName = string.Empty,
                           Status = order.Status,
                       }).ToList()
                   });

            var paged = await _unitOfWork.Organizations.GetByPageFilter(
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

            var enrichmentTasks = new List<Task>();
            foreach (var dto in dtoItems)
            {
                foreach (var sub in dto.Subscriptions)
                {
                    var subscription = sub;
                    enrichmentTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var plan = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(subscription.PlanBillingCycleId, cancellationToken);
                            subscription.PlanBillingCycle = plan.BillingCycle.ToString();
                            subscription.PlanName = plan.Name;
                        }
                        catch
                        {
                            subscription.PlanBillingCycle = string.Empty;
                            subscription.PlanName = string.Empty;
                        }
                    }, cancellationToken));
                }
            }

            if (enrichmentTasks.Count > 0)
                await Task.WhenAll(enrichmentTasks);

            var organizationDetails = dtoItems.Select(dto =>
            {
                var detail = new GrpcOrganizationDetail
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Code = dto.Code,
                    Description = dto.Description,
                    ImageUrl = dto.ImageUrl,
                    OrganizationType = dto.OrganizationType,
                    Status = dto.Status.ToString(),
                    CreatedDate = Timestamp.FromDateTimeOffset(dto.CreatedDate),
                    LastModifiedDate = dto.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(dto.LastModifiedDate.Value)
                        : null
                };

                // Map SubscriptionDto -> GrpcSubscriptionModel and add to repeated field
                var subs = dto.Subscriptions.Select(s => new GrpcSubscriptionModel
                {
                    Id = s.Id,
                    PlanName = s.PlanName ?? string.Empty,
                    Code = s.Code ?? string.Empty,
                    PlanBillingCycle = s.PlanBillingCycle ?? string.Empty,
                    GrossAmount = (double)s.GrossAmount,
                    NetAmount = (double)s.NetAmount,
                    Status = s.Status.ToString(),
                    StartDate = Timestamp.FromDateTime(s.StartDate.ToUniversalTime()),
                    EndDate = Timestamp.FromDateTime(s.EndDate.ToUniversalTime()),
                    MaxStudentSeats = s.MaxStudentSeats,
                    MaxTeacherSeats = s.MaxTeacherSeats,
                    CurrentStudentSeats = s.CurrentStudentSeats,
                    CurrentTeacherSeats = s.CurrentTeacherSeats
                });

                detail.Subscriptions.AddRange(subs);

                return detail;
            }).ToList();

            var response = new GrpcPagedOrganizationResponse
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