using Google.Protobuf.WellKnownTypes;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Specifications;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationById
{
    public class GetOrganizationByIdQueryHandler
        : IRequestHandler<GetOrganizationByIdQuery, GrpcOrganizationDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;

        public GetOrganizationByIdQueryHandler(IOrderUnitOfWork unitOfWork, IPlanBillingCycleCacheService planBillingCycleCacheService)
        {
            _unitOfWork = unitOfWork;
            _planBillingCycleCacheService = planBillingCycleCacheService;
        }

        public async Task<GrpcOrganizationDetail> Handle(
            GetOrganizationByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new OrganizationByIdSpecification(request.Id);

            var organization = await _unitOfWork.Organizations.FirstOrDefaultAsync(spec, cancellationToken);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {request.Id} not found.");
            }

            var grpcOrganization = new GrpcOrganizationDetail
            {
                Id = organization.Id,
                Name = organization.Name,
                Description = organization.Description ?? string.Empty,
                ImageUrl = organization.ImageUrl ?? string.Empty,
                Status = organization.Status.ToString(),
                OrganizationType = organization.OrganizationType.Name,
                Code = organization.Code,
                CreatedDate = Timestamp.FromDateTimeOffset(organization.CreatedDate),
                LastModifiedDate = organization.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(organization.LastModifiedDate.Value)
                        : null,
            };

            var subscriptionTasks = (organization.SubscriptionOrders ?? Enumerable.Empty<Domain.Entities.OrganizationSubscriptionOrder>())
                .OrderByDescending(s => s.StartDate)
                .ThenByDescending(s => s.CreatedDate)
                .Select(async s =>
                {
                    var planBillingCycle = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(s.PlanBillingCycleId, cancellationToken);
                    if (planBillingCycle == null)
                    {
                        throw new KeyNotFoundException($"PlanBillingCycle with ID {s.PlanBillingCycleId} not found in cache.");
                    }

                    return new GrpcSubscriptionModel
                    {
                        Id = s.Id,
                        PlanName = planBillingCycle.Name,
                        Code = s.Code,
                        PlanBillingCycle = planBillingCycle.BillingCycle.ToString(),
                        GrossAmount = (double)s.GrossAmount,
                        NetAmount = (double)s.NetAmount,
                        Status = s.Status.ToString(),
                        StartDate = Timestamp.FromDateTime(s.StartDate.ToUniversalTime()),
                        EndDate = Timestamp.FromDateTime(s.EndDate.ToUniversalTime()),
                        MaxStudentSeats = s.MaxStudentSeats,
                        MaxTeacherSeats = s.MaxTeacherSeats,
                        CurrentStudentSeats = s.LicenseAssignments?.Count(la =>
                            la.OrganizationSubscriptionOrderId == s.Id &&
                            la.LicenseType == Domain.Enums.LicenseType.Student &&
                            (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)) ?? 0,
                        CurrentTeacherSeats = s.LicenseAssignments?.Count(la =>
                            la.OrganizationSubscriptionOrderId == s.Id &&
                            la.LicenseType == Domain.Enums.LicenseType.Teacher &&
                            (la.Status == Domain.Enums.LicenseAssignmentStatus.Active || la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)) ?? 0,
                    };
                })
                .ToList();

            var subs = await Task.WhenAll(subscriptionTasks);

            grpcOrganization.Subscriptions.AddRange(subs);

            return grpcOrganization;
        }
    }
}