using Ardalis.Specification;
using Order.Domain.Entities;

namespace Order.Application.Specifications
{
    public class ContractByIdSpecification : Specification<Contract>
    {
        public ContractByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id)
                .Include(c => c.Organization)
                    .ThenInclude(o => o.OrganizationType)
                ;
        }
    }

    public class ContractWithIncludesSpecification : Specification<Contract>
    {
        public ContractWithIncludesSpecification()
        {
            Query
                .Include(c => c.Organization)
                    .ThenInclude(o => o.OrganizationType);
        }
    }
}
