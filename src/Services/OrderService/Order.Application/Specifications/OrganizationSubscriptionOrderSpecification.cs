using Ardalis.Specification;
using Order.Domain.Entities;
using Order.Domain.Enums;

namespace Order.Application.Specifications
{
    public class OrganizationSubscriptionOrderByIdSpecification : Specification<OrganizationSubscriptionOrder>
    {
        public OrganizationSubscriptionOrderByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Organization)
                    .ThenInclude(o => o.OrganizationType)
                .Include(c => c.LicenseAssignments)
                .Include(c => c.SubscriptionOrderCurriculums)
                .Include(c => c.Contract);
        }
    }

    public class OrganizationSubscriptionOrderByOrganizationIdSpecification : Specification<OrganizationSubscriptionOrder>
    {
        public OrganizationSubscriptionOrderByOrganizationIdSpecification(int id)
        {
            Query.Where(c => c.OrganizationId == id)
                .Include(c => c.Organization)
                    .ThenInclude(o => o.OrganizationType)
                .Include(c => c.LicenseAssignments)
                .Include(c => c.SubscriptionOrderCurriculums)
                .Include(c => c.Contract);
        }
    }

    public class OrganizationSubscriptionOrderWithIncludesSpecification : Specification<OrganizationSubscriptionOrder>
    {
        public OrganizationSubscriptionOrderWithIncludesSpecification()
        {
            Query
                .Include(c => c.LicenseAssignments)
                .Include(x => x.Contract);
        }
    }

    public class PendingOrganizationSubscriptionOrdersReadyForActivationSpecification
        : Specification<OrganizationSubscriptionOrder>
    {
        public PendingOrganizationSubscriptionOrdersReadyForActivationSpecification(DateTime executionTimeUtc)
        {
            Query
                .Where(order =>
                    order.Status == OrganizationSubscriptionOrderStatus.Pending &&
                    order.StartDate <= executionTimeUtc)
                .Include(order => order.Organization);
        }
    }

    public class ActiveOrganizationSubscriptionOrdersReadyForExpirationSpecification
        : Specification<OrganizationSubscriptionOrder>
    {
        public ActiveOrganizationSubscriptionOrdersReadyForExpirationSpecification(DateTime executionTimeUtc)
        {
            Query
                .Where(order =>
                    order.Status == OrganizationSubscriptionOrderStatus.Active &&
                    order.EndDate < executionTimeUtc)
                .Include(order => order.Organization);
        }
    }

    public class OrganizationCurriculumByOrganizationIdSpecification : Specification<OrganizationSubscriptionOrder>
    {
        public OrganizationCurriculumByOrganizationIdSpecification(int organizationId, OrganizationSubscriptionOrderStatus status)
        {
            Query
                .Where(o => o.OrganizationId == organizationId && o.Status == status)
                .Include(o => o.SubscriptionOrderCurriculums);
        }
    }

    public class OrganizationSubscriptionOrdersWithCurriculumsSpec : Specification<OrganizationSubscriptionOrder>
    {
        public OrganizationSubscriptionOrdersWithCurriculumsSpec(int organizationId, DateTimeOffset start, DateTimeOffset end)
        {
            Query
                .Where(o => o.OrganizationId == organizationId && o.CreatedDate >= start && o.CreatedDate < end)
                .Include(o => o.SubscriptionOrderCurriculums);
        }
    }
}
