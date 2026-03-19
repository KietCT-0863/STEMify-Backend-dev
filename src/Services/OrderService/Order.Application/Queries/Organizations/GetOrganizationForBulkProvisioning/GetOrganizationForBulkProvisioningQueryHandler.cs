using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Specifications;
using Shared.Protos.Order;

namespace Order.Application.Queries.Organizations.GetOrganizationForBulkProvisioning;

public class GetOrganizationForBulkProvisioningQueryHandler
    : IRequestHandler<GetOrganizationForBulkProvisioningQuery, GrpcOrganizationBulkProvisioningInfo>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;

    public GetOrganizationForBulkProvisioningQueryHandler(
        IOrderUnitOfWork unitOfWork,
        IPlanBillingCycleCacheService planBillingCycleCacheService)
    {
        _unitOfWork = unitOfWork;
        _planBillingCycleCacheService = planBillingCycleCacheService;
    }

    public async Task<GrpcOrganizationBulkProvisioningInfo> Handle(
        GetOrganizationForBulkProvisioningQuery request,
        CancellationToken cancellationToken)
    {
        try {
            var spec = new OrganizationByIdSpecification(request.Id);
            var organization = await _unitOfWork.Organizations.FirstOrDefaultAsync(spec, cancellationToken);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {request.Id} not found.");
            }

            var response = new GrpcOrganizationBulkProvisioningInfo
            {
                Id = organization.Id,
                Name = organization.Name,
                EmailDomain = string.Empty,
                Status = organization.Status.ToString(),
                IsActive = organization.Status == Domain.Enums.OrganizationStatus.Active
            };

            // Get active subscriptions with license information
            var activeSubscriptions = (organization.SubscriptionOrders ?? Enumerable.Empty<Domain.Entities.OrganizationSubscriptionOrder>())
                .Where(s => (s.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Active || s.Status == Domain.Enums.OrganizationSubscriptionOrderStatus.Pending))
                .OrderByDescending(s => s.StartDate)
                .ThenByDescending(s => s.CreatedDate);

            var subscriptionTasks = activeSubscriptions
                .Select(async s =>
                {
                    var planBillingCycle = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(
                        s.PlanBillingCycleId,
                        cancellationToken);

                    if (planBillingCycle == null)
                    {
                        throw new KeyNotFoundException($"PlanBillingCycle with ID {s.PlanBillingCycleId} not found in cache.");
                    }

                    // Count current active/pending license assignments
                    var currentStudentSeats = s.LicenseAssignments?.Count(la =>
                        la.OrganizationSubscriptionOrderId == s.Id &&
                        la.LicenseType == Domain.Enums.LicenseType.Student &&
                        (la.Status == Domain.Enums.LicenseAssignmentStatus.Active ||
                         la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)) ?? 0;

                    var currentTeacherSeats = s.LicenseAssignments?.Count(la =>
                        la.OrganizationSubscriptionOrderId == s.Id &&
                        la.LicenseType == Domain.Enums.LicenseType.Teacher &&
                        (la.Status == Domain.Enums.LicenseAssignmentStatus.Active ||
                         la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)) ?? 0;

                    var currentOrgAdminSeats = s.LicenseAssignments?.Count(la =>
                        la.OrganizationSubscriptionOrderId == s.Id &&
                        la.LicenseType == Domain.Enums.LicenseType.OrganizationAdmin &&
                        (la.Status == Domain.Enums.LicenseAssignmentStatus.Active ||
                         la.Status == Domain.Enums.LicenseAssignmentStatus.Pending)) ?? 0;

                    // Note: OrganizationSubscriptionOrder entity doesn't have MaxOrganizationAdminSeats property yet
                    // Using default value of 10 until the property is added to the entity
                    const int defaultMaxOrgAdminSeats = 10;

                    return new GrpcSubscriptionLicenseInfo
                    {
                        SubscriptionOrderId = s.Id,
                        PlanName = planBillingCycle.Name,
                        Status = s.Status.ToString(),
                        MaxStudentSeats = s.MaxStudentSeats,
                        MaxTeacherSeats = s.MaxTeacherSeats,
                        MaxOrganizationAdminSeats = defaultMaxOrgAdminSeats,
                        CurrentStudentSeats = currentStudentSeats,
                        CurrentTeacherSeats = currentTeacherSeats,
                        CurrentOrganizationAdminSeats = currentOrgAdminSeats,
                        AvailableStudentSeats = s.MaxStudentSeats - currentStudentSeats,
                        AvailableTeacherSeats = s.MaxTeacherSeats - currentTeacherSeats,
                        AvailableOrganizationAdminSeats = defaultMaxOrgAdminSeats - currentOrgAdminSeats
                    };
                })
                .ToList();

            var subscriptions = await Task.WhenAll(subscriptionTasks);
            response.Subscriptions.AddRange(subscriptions);

            return response;
        
        }
        catch ( Exception ex ) {
            Console.WriteLine(ex);
            throw;
        }
       
    }
}
