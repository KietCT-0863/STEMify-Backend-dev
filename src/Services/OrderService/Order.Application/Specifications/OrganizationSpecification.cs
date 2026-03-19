using Ardalis.Specification;
using Order.Domain.Entities;

namespace Order.Application.Specifications
{
    public class OrganizationByIdSpecification : Specification<Organization>
    {
        public OrganizationByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.SubscriptionOrders)
                    .ThenInclude(s => s.LicenseAssignments)
                .Include(c => c.OrganizationType);
        }
    }

    public class OrganizationWithIncludesSpecification : Specification<Organization>
    {
        public OrganizationWithIncludesSpecification()
        {
            Query.Include(x => x.OrganizationType);
        }
    }
}
