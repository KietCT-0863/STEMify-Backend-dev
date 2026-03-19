using Ardalis.Specification;
using Order.Domain.Entities;
using Order.Domain.Enums;

namespace Order.Application.Specifications
{
    public class LicenseAssignmentByIdSpecification : Specification<LicenseAssignment>
    {
        public LicenseAssignmentByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                 .Include(c => c.OrganizationSubscriptionOrder);
        }
    }

    public class LicenseAssignmentWithIncludesSpecification : Specification<LicenseAssignment>
    {
        public LicenseAssignmentWithIncludesSpecification()
        {
            Query.Include(c => c.OrganizationSubscriptionOrder);
        }
    }

    public class LicenseAssignmentBySubscriptionUserAndTypeSpecification : Specification<LicenseAssignment>
    {
        public LicenseAssignmentBySubscriptionUserAndTypeSpecification(int subscriptionOrderId, string userId, LicenseType licenseType)
        {
            Query.Where(c =>
                c.OrganizationSubscriptionOrderId == subscriptionOrderId &&
                c.OrganizationUserId == userId &&
                c.LicenseType == licenseType);
        }
    }

    public class LicenseAssignmentsBySubscriptionSpecification : Specification<LicenseAssignment>
    {
        public LicenseAssignmentsBySubscriptionSpecification(int subscriptionOrderId, LicenseType? licenseType = null, params LicenseAssignmentStatus[] statuses)
        {
            Query.Where(c => c.OrganizationSubscriptionOrderId == subscriptionOrderId);

            if (licenseType.HasValue)
                Query.Where(c => c.LicenseType == licenseType.Value);

            if (statuses != null && statuses.Length > 0)
                Query.Where(c => statuses.Contains(c.Status));

            Query.Include(c => c.OrganizationSubscriptionOrder);
        }
    }

    public class LicenseAssignmentsByUserSpecification : Specification<LicenseAssignment>
    {
        public LicenseAssignmentsByUserSpecification(List<string> orgUserIds, int? subscriptionOrderId = null, LicenseAssignmentStatus? status = null)
        {
            Query.Where(c => orgUserIds.Contains(c.OrganizationUserId))
                 .Include(l => l.OrganizationSubscriptionOrder)
                    .ThenInclude(s => s.SubscriptionOrderCurriculums)
                .Include(l => l.OrganizationSubscriptionOrder)
                    .ThenInclude(s => s.Organization);

            if (subscriptionOrderId.HasValue)
                Query.Where(c => c.OrganizationSubscriptionOrderId == subscriptionOrderId.Value);
            if (status.HasValue)
                Query.Where(c => c.Status == status.Value);
        }
    }

    public class ActiveOrPendingBySubscriptionAndTypeSpecification : Specification<LicenseAssignment>
    {
        public ActiveOrPendingBySubscriptionAndTypeSpecification(int subscriptionOrderId, LicenseType licenseType)
        {
            Query.Where(c =>
                c.OrganizationSubscriptionOrderId == subscriptionOrderId &&
                c.LicenseType == licenseType &&
                (c.Status == LicenseAssignmentStatus.Active || c.Status == LicenseAssignmentStatus.Pending));
        }
    }
}